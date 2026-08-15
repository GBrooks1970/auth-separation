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
