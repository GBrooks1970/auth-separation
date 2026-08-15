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
