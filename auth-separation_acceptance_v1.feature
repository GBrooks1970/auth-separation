# Acceptance criteria for the Auth Separation example
#
# These scenarios are the executable definition of "production-ready" for a
# skeleton built from the specs in this folder. Every scenario must pass
# end-to-end against a running stack of the three services and the event
# bus before the skeleton can be considered complete.

Feature: Account registration and first login
  As a new user
  I want to register an account and log in
  So that I can use the system

  Background:
    Given the AuthN service is running
    And the AuthZ service is running
    And the User Info service is running
    And the event bus is running and consumers are connected

  Scenario: Successful registration
    When I POST to AuthN /users with a valid login_identifier and password
    Then the response status is 201
    And the response contains a user_id
    And a UserRegistered event is published on user.lifecycle within 2 seconds
    And the User Info service has created an empty profile keyed by that user_id
    And the AuthZ service has no role assignments for that user_id

  Scenario: Registration with an existing identifier is rejected
    Given a user already exists with login_identifier "alex@example.com"
    When I POST to AuthN /users with login_identifier "alex@example.com"
    Then the response status is 409
    And the response code field is "identifier_in_use"

  Scenario: Login returns a valid token pair
    Given a registered user with valid credentials
    When I POST to AuthN /sessions with those credentials
    Then the response status is 200
    And the response contains an access_token, a refresh_token, and expires_in
    And the access_token is signed with the key advertised at /.well-known/jwks.json
    And the access_token contains no role or permission claims

  Scenario: Login with bad password is rejected
    Given a registered user with login_identifier "alex@example.com"
    When I POST to AuthN /sessions with login_identifier "alex@example.com" and a wrong password
    Then the response status is 401
    And no token is returned
    And an AuthenticationFailed event is published with reason "bad_password"

Feature: Authorisation decisions are independent of tokens

  Scenario: A freshly issued token reflects no permissions until AuthZ is called
    Given a user with no roles assigned
    And the user holds a valid access token
    When the user requests a protected resource that requires "invoice.read"
    Then the resource server calls AuthZ /decisions/check
    And the AuthZ response is allowed=false
    And the resource server returns 403 to the user

  Scenario: A role assignment takes effect on the next request without re-issuing the token
    Given a user with no roles assigned holding a valid access token
    And the user has just received 403 for "invoice.read"
    When an admin POSTs to AuthZ /users/{userId}/roles assigning a role with "invoice.read"
    And the user retries the protected request with the same access token
    Then the AuthZ response is allowed=true
    And the resource server returns 200

  Scenario: A revoked role denies the next request
    Given a user holding a valid access token with a role granting "invoice.read"
    When an admin DELETEs the role assignment
    And the user retries the protected request with the same access token
    Then the AuthZ response is allowed=false
    And the resource server returns 403

Feature: User Info ownership and access control

  Scenario: A user can read and update their own profile
    Given a registered user with a valid access token
    When the user GETs User Info /users/me
    Then the response status is 200
    And the response includes the user_id and any populated profile fields
    When the user PATCHes /users/me with a new display_name
    Then the response status is 200
    And the response display_name matches the patched value
    And a ProfileUpdated event is published listing "display_name" in fields_changed

  Scenario: A user cannot read another user's full profile without authorisation
    Given users Alex and Sam, both registered, with no shared role
    And Alex holds a valid access token
    When Alex GETs User Info /users/{samUserId}
    Then User Info calls AuthZ /decisions/check for action "user.read" on that user_id
    And AuthZ returns allowed=false
    And User Info returns 403

  Scenario: An admin can read another user's profile when authorised
    Given users Alex and Sam, both registered
    And Alex has been assigned a role granting "user.read"
    And Alex holds a valid access token
    When Alex GETs User Info /users/{samUserId}
    Then AuthZ returns allowed=true
    And User Info returns 200 with Sam's profile fields

Feature: Multi-factor authentication

  Scenario: First-time login of an MFA-enrolled user is challenged
    Given a registered user with TOTP MFA enrolled
    When the user POSTs to AuthN /sessions with valid credentials
    Then the response status is 403
    And the response contains an MfaChallenge with a challenge_id and methods including "totp"

  Scenario: Submitting a valid TOTP completes the login
    Given a user has just received an MfaChallenge for a valid login
    When the user POSTs to AuthN /mfa/challenge with the challenge_id and a valid TOTP code
    Then the response status is 200
    And the response contains an access_token and refresh_token
    And the access_token claim "mfa" is true

  Scenario: Submitting an invalid TOTP is rejected
    Given a user has just received an MfaChallenge for a valid login
    When the user POSTs to AuthN /mfa/challenge with the challenge_id and an invalid code
    Then the response status is 401
    And no token is returned

Feature: Token lifecycle

  Scenario: Refreshing a session rotates the refresh token
    Given a user holds a valid refresh_token RT1
    When the user POSTs to AuthN /sessions/refresh with RT1
    Then the response status is 200
    And the response contains a new access_token and a new refresh_token RT2
    And RT2 is different from RT1
    When the user POSTs to AuthN /sessions/refresh with RT1 again
    Then the response status is 401

  Scenario: Logging out revokes the refresh token
    Given a user holds a valid access_token and refresh_token
    When the user DELETEs AuthN /sessions
    Then the response status is 204
    And subsequent /sessions/refresh with that refresh_token returns 401
    And a SessionRevoked event is published with reason "user_logout"

  Scenario: Changing password revokes all other sessions
    Given a user has two active sessions on two devices
    When the user PUTs AuthN /credentials/password with current and new password
    Then the response status is 204
    And SessionRevoked events are published for both prior sessions
    And the device that initiated the change retains its current session

Feature: Consent and lifecycle

  Scenario: Granting consent records the version agreed to
    Given a registered user with a valid access token
    When the user POSTs User Info /users/me/consents with key "marketing.email" and version "2026-03"
    Then the response status is 201
    And the stored ConsentGrant records granted_at and version "2026-03"
    And a ConsentGranted event is published

  Scenario: Withdrawing consent emits the corresponding event
    Given the user has previously granted "marketing.email"
    When the user DELETEs User Info /users/me/consents/marketing.email
    Then the response status is 204
    And a ConsentWithdrawn event is published with key "marketing.email"

  Scenario: Account deletion propagates across all three services
    Given a registered user with a valid access token, profile data, and assigned roles
    When the user DELETEs User Info /users/me
    Then the response status is 202
    And a UserDeletionRequested event is published
    And within 60 seconds:
      | service   | observable result                              |
      | authz     | all role assignments for the user are removed  |
      | authn     | all sessions are revoked and credentials wiped |
      | userinfo  | PII columns are scrubbed; audit shell remains  |
    And a UserDeleted event is published listing all three services

Feature: Contract conformance

  Scenario: Every endpoint response validates against its OpenAPI schema
    Given the contract verification suite is configured against all three OpenAPI files
    When the suite runs against the deployed services
    Then no validation failures are reported

  Scenario: Every published event validates against its AsyncAPI schema
    Given the event verification suite is configured against the AsyncAPI file
    When the suite tails the bus during the acceptance run
    Then no event payload fails schema validation
