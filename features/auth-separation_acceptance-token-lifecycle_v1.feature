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
