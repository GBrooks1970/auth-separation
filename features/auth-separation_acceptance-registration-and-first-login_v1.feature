Feature: Account registration and first login
  As a new user
  I want to register an account and log in
  So that I can use the system

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
