namespace Orleans.Providers.Firebase.Authentication
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Newtonsoft.Json;
    using Org.BouncyCastle.Crypto.Parameters;
    using Org.BouncyCastle.OpenSsl;

    public class FirebaseTokenRefresher
    {
        private static readonly char[] Base64Padding = { '=' };
        private static readonly DateTime Jan1st1970 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public async Task<string> RefreshTokenAsync(HttpClient httpClient, FirebaseServiceKey key)
        {
            var header = new FirebaseTokenRequestHeader
            {
                Alg = "RS256",
                Kid = key.PrivateKeyId,
                Typ = "JWT"
            };

            var payload = new FirebaseTokenRequestPayload
            {
                Aud = key.TokenUri,
                Exp = (TimeNowInMilliseconds() / 1000) + 3600,
                Iat = TimeNowInMilliseconds() / 1000,
                Iss = key.ClientEmail,
                Scope = "https://www.googleapis.com/auth/firebase.database https://www.googleapis.com/auth/userinfo.email"
            };

            var content = $"{SerializeAndBase64Safe(header)}.{SerializeAndBase64Safe(payload)}";

            var contentBytes = Encoding.UTF8.GetBytes(content);

            RsaPrivateCrtKeyParameters rsaPrivKey;

            using (var reader = new StringReader(key.PrivateKey))
            {
                rsaPrivKey = (RsaPrivateCrtKeyParameters)new PemReader(reader).ReadObject();
            }

            var rsa = RSA.Create();

            var rsaParameters = ToRSAParameters(rsaPrivKey);

            rsa.ImportParameters(rsaParameters);

            var sha = SHA256.Create();

            byte[] hash = sha.ComputeHash(contentBytes);

            var b64signature = Base64Safe(rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1));

            var assertion = $"{content}.{b64signature}";

            var body = new Dictionary<string, string>
            {
                { "grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer" },
                { "assertion", assertion }
            };

            var token = default(CancellationToken);

            var response = await httpClient.PostAsync(key.TokenUri, new FormUrlEncodedContent(body), token);

            var responseContent = await response.Content.ReadAsStringAsync();

            var tokenResponse = JsonConvert.DeserializeObject<FirebaseTokenResponse>(responseContent);

            return tokenResponse.AccessToken;
        }

        private static string Base64Safe(byte[] bytes)
        {
            return Convert.ToBase64String(bytes).TrimEnd(Base64Padding).Replace('+', '-').Replace('/', '_');
        }

        private static string SerializeAndBase64Safe(object value)
        {
            return Base64Safe(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value)));
        }

        private static long TimeNowInMilliseconds()
        {
            return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
        }

        private static RSAParameters ToRSAParameters(RsaPrivateCrtKeyParameters privKey)
        {
            return new RSAParameters
            {
                Modulus = privKey.Modulus.ToByteArrayUnsigned(),
                Exponent = privKey.PublicExponent.ToByteArrayUnsigned(),
                D = privKey.Exponent.ToByteArrayUnsigned(),
                P = privKey.P.ToByteArrayUnsigned(),
                Q = privKey.Q.ToByteArrayUnsigned(),
                DP = privKey.DP.ToByteArrayUnsigned(),
                DQ = privKey.DQ.ToByteArrayUnsigned(),
                InverseQ = privKey.QInv.ToByteArrayUnsigned()
            };
        }
    }
}
