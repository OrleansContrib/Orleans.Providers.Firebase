# Orleans.Providers.Firebase
An implementation of the Orleans storage and membership provider models for Firebase realtime database.
> This provider library is in early development and is not recommended for production usage.

## Usage
### Host Configuration

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
    <SystemStore SystemStoreType="Custom" DataConnectionString="https://{yourfirebasedatabase}.firebaseio.com" MembershipTableAssembly="Orleans.Providers.Firebase" ReminderServiceType="ReminderTableGrain" ReminderTableAssembly="Orleans.Providers.Firebase"/>
  </Globals>
</OrleansConfiguration>
```

### Examples
See the *Orleans.Providers.Firebase.Tests.Host* project for example usage.
