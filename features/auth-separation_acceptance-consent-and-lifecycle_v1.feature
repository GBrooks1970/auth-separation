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
