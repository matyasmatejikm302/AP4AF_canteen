Keycloak integration notes

1) Configure Keycloak server and realm.
2) Create client `canteen-client` of type `public` or `confidential` depending on flow.
3) For WebAssembly (OIDC): use Authorization Code Flow with PKCE.
4) Set appsettings.json in client wwwroot with Authority and ClientId.
5) Backend: configure audience/client-id in AddKeycloakJwtAuthentication("keycloak") in ServiceDefaults. Set `keycloak` section in server appsettings with Authority and Audience.

If you want, provide me your Keycloak Authority and ClientId and I will inject them into appsettings files in the repo.