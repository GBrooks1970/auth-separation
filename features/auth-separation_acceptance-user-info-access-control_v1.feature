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
