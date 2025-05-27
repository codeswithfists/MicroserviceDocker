# MicroserviceDocker

Basic microservice architecture using YARP as an API gateway and Keycloak for authentication. From the gateway service, navigate to /scalar to view the OpenApi documentation.

Multiple OAuth2 authentication flow configurations are demonstrated for Api1:

- Authorization Code Flow (PKCE)
- Client Credentials Flow
- Resource Owner Password Credentials Flow
- Implicit Flow

![image](https://github.com/user-attachments/assets/f534d969-b675-45fe-ab64-efabba7d21bc)

There is also a [Bruno](https://www.usebruno.com/) collection for testing the API endpoints.

> [!WARNING]
> Didn't really test the Docker side of things too much, but it should mostly work - will require some config changes. Also note that Docker is not currently setup to use https.

## Keycloak Notes

* 4 Clients: apigateway, api1, api2, blazorapp
    - api1 has the 'special' role
* 2 Client scopes: api1_scope, api2_scope
    - both scopes should have Audience mapper for respective client
* 2 Realm roles: admin, user
* 2 Users: boss, worker
    - boss roles = user, admin
    - worker roles = user, api1.special

> [!TIP]
> The provided `realm-shadow.json` file can be imported to start, but you will need to still add/modify a few things. ie. create the `api1_scope` and `api2_scope` client scopes with the associated audience mappers.

You will likely need to regenerate the client secret for the `apigateway` client.

---
Temp notes:

- Modified the 'client roles' and 'realm roles' mappers in the 'roles' Client scope to combine the roles from both groups into a single 'roles' array property on the token. This is where .NET Authorization looks for roles.
- For a user, if a role mapping is assigned for a client role (ie. not a realm role), then it seems as though that client is added to the 'aud' claim regardless of any scopes requested.
- There is an Audience Resolve mapper on the 'roles' client scope - not sure if that's there by default?
