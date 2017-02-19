# Orleans.Providers.Firebase
An implementation of the Orleans storage provider model for Firebase realtime database.
> This provider library is in early development and is not recommended for production usage.

## Usage
###Host Configuration

```ps
Manually reference the Orleans.Providers.Firebase project (NuGet package not yet available).
```
Update OrleansConfiguration.xml in the Host application to add the following (example configuration)...
```xml
<OrleansConfiguration xmlns="urn:orleans">
  <Globals>
    <StorageProviders>
      <Provider Type="Orleans.Providers.Firebase.Storage.FirebaseStorageProvider" Name="Default" BasePath="https://{yourfirebasedatabase}.firebaseio.com" Auth="{yourfirebaseauth}"/>
    </StorageProviders>
    ...
  </Globals>
</OrleansConfiguration>
```

###Examples
See the *Orleans.Providers.Firebase.Tests.Host* project for example usage.
