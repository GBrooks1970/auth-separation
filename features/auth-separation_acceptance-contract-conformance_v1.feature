Feature: Contract conformance

  Scenario: Every endpoint response validates against its OpenAPI schema
    Given the contract verification suite is configured against all three OpenAPI files
    When the suite runs against the deployed services
    Then no validation failures are reported

  Scenario: Every published event validates against its AsyncAPI schema
    Given the event verification suite is configured against the AsyncAPI file
    When the suite tails the bus during the acceptance run
    Then no event payload fails schema validation
