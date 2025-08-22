# Spec Summary (Lite)

Integrate the Platform BFF with the existing auth-service using OpenID Connect to enable secure authentication via server-side token management. The BFF will act as an OIDC client, storing tokens in Redis sessions and exposing only secure HttpOnly cookies to the browser, ensuring complete token isolation from client-side JavaScript.