# Design Doc: Deployment Topology for the Three Services

**Status:** Proposed
**Decision date:** TBD
**Decision owner:** Platform engineering lead
**Affects:** AuthN, AuthZ, User Info services
**Supersedes:** None
**Related:** `auth-separation_architecture_v1.md`, the three compliance docs in this folder

---

## 1. Context

The architecture document establishes that AuthN, AuthZ, and User Info are three independently deployable services, each owning a disjoint slice of user data, each with its own trust boundary. The architecture document does not say where those services run or how they are packaged. This design doc addresses that gap.

The proposal under consideration is: **deploy each of the three services in its own Docker instance, on its own dedicated server (one container, one host, three hosts in total)**. This is the strongest possible interpretation of physical separation. It is not the only interpretation, and not necessarily the right one for every deployment, but it is a defensible default and a useful starting point because it makes every separation argument concrete: a separate process, a separate kernel, a separate machine.

This document posits that approach, reasons through its positives and negatives, and surveys the alternatives so that a deployment team can choose with eyes open rather than by default.

---

## 2. The proposal

```
+------------------------------+   +------------------------------+   +------------------------------+
|        Server A              |   |        Server B              |   |        Server C              |
|                              |   |                              |   |                              |
|   +---------------------+    |   |   +---------------------+    |   |   +---------------------+    |
|   |   Docker daemon     |    |   |   |   Docker daemon     |    |   |   |   Docker daemon     |    |
|   |                     |    |   |   |                     |    |   |   |                     |    |
|   |  +---------------+  |    |   |   |  +---------------+  |    |   |   |  +---------------+  |    |
|   |  |  AuthN        |  |    |   |   |  |  AuthZ        |  |    |   |   |  |  User Info    |  |    |
|   |  |  container    |  |    |   |   |  |  container    |  |    |   |   |  |  container    |  |    |
|   |  +---------------+  |    |   |   |  +---------------+  |    |   |   |  +---------------+  |    |
|   +---------------------+    |   |   +---------------------+    |   |   +---------------------+    |
|                              |   |                              |   |                              |
|   Credential DB              |   |   Role/Policy DB             |   |   Profile DB                 |
|   KMS (HSM-backed)           |   |   Audit log store            |   |   Encrypted PII column store |
+------------------------------+   +------------------------------+   +------------------------------+
                              \\                |               //
                               \\               |              //
                                +-------------- + -------------+
                                          Internal mesh
                                          (mTLS only)
                                                |
                                          +----------------+
                                          |  Message bus   |
                                          | (separate host)|
                                          +----------------+
```

Each service has:

- One Docker container running the service binary.
- One dedicated host (bare metal or VM) running one Docker daemon hosting one production container.
- Its own database, on the same host or on a dedicated database host paired with the service host.
- Its own outbound and inbound firewall rules.
- Its own operational team or named owner.

The three hosts communicate only through the published HTTP contracts and the message bus. There is no shared filesystem, no shared database, no shared process space.

---

## 3. Positives

The argument for this topology is not "Docker is good." It is "the strongest possible isolation between three high-stakes services." Docker is a means; isolation is the end.

### 3.1 Blast radius is bounded by hardware

A kernel exploit, a container escape, a runtime vulnerability, or a misconfigured volume mount stops at the host boundary. An attacker who lands code execution inside the AuthN container has access to the AuthN host. They do not have lateral access to AuthZ or User Info, because those processes are not on the same kernel, the same memory space, or the same disk. The attacker must traverse the network to reach the next service, where they encounter mTLS, firewall rules, and the published API as their only entry point.

This is the strongest formulation of the architecture document's separation argument. Co-locating all three on one machine weakens it; co-locating two on one machine weakens it for those two.

### 3.2 Compliance scope is physically demonstrable

The three compliance docs in this folder (PCI-style for AuthN, GDPR for User Info, SOC 2 for AuthZ) all rest on the claim that each compliance regime sits within the boundary of one service. With three hosts, that boundary is physical. A PCI assessor can point to Server A and say "this is the credential boundary." A GDPR assessor can point to Server C and say "this is the personal data boundary." A SOC 2 examiner can point to Server B and say "this is the access decision boundary." There is no ambiguity, no shared substrate, no "it depends on which container is running."

### 3.3 Independent failure modes

A failed deployment of the AuthZ service does not affect the AuthN process. A memory leak in User Info does not starve AuthN of resources. A noisy-neighbour container cannot starve another service of CPU because there is no neighbour.

### 3.4 Independent scaling characteristics

The three services have different load profiles. AuthN is bursty (login storms during business hours, near-zero overnight). AuthZ is constant (every protected request hits it). User Info is read-heavy with infrequent writes. With independent hosts, each service can be scaled, sized, and instance-typed for its actual workload. Co-located services would have to share a sizing decision that fits the worst-case overlap.

### 3.5 Independent technology choices

The three services do not need to share a runtime, a base image, a language, or a framework. AuthN may need a runtime with strong cryptographic libraries; AuthZ may benefit from a low-latency runtime; User Info may use a runtime well-suited to encryption-at-rest workflows. Each container ships only what its service needs.

### 3.6 Independent change cadence

A security patch to AuthN (high urgency, fast rollout) does not have to wait for an unrelated AuthZ feature release. The deployment pipelines are independent because the deployment targets are independent.

### 3.7 Operational ownership is unambiguous

Each host has one team named on the runbook. There is no shared host where two teams have to coordinate before any change. Pager rotations, access lists, and break-glass procedures are scoped to one service per host.

### 3.8 Audit and observability are simpler at the boundary

Logs from each host can be tagged with service identity at the source rather than relying on container metadata that may be wrong, missing, or spoofed. Network packet captures show only one service's traffic per host. Forensic analysis after an incident does not need to disentangle interleaved logs from co-located services.

---

## 4. Negatives

The proposal pays a real price for its isolation. Calling these out honestly is the point of the design doc.

### 4.1 Infrastructure cost

Three hosts cost more than one. For low-traffic deployments, the absolute cost is small but the percentage overhead is large: three hosts, three operating system licences (if applicable), three sets of base monitoring agents, three patching cycles. A single host running three containers would absorb the same workload at a fraction of the bill.

For a deployment with predictable load, this is wasteful. For a deployment expecting bursty growth, it is acceptable. For a tiny internal deployment, it is overkill.

### 4.2 Operational overhead

Three hosts require three host-level operating system patches, three sets of security updates, three configuration drift checks, three log forwarders, three monitoring agents. A small team feels this disproportionately. Each new infrastructure change has to be applied three times rather than once, and the chance of inconsistency between hosts grows with every change.

### 4.3 Network latency between services

Every cross-service call (every request to the protected resource API hits AuthZ; many hit User Info too) crosses a network boundary. Same-data-centre latency is typically 0.5 to 2 milliseconds, but it is non-zero, and it is variable. Co-located services on a shared host communicate at process or loopback speed, which is hundreds of microseconds at most.

For the auth-separation pattern specifically this matters because AuthZ is on the hot path of every protected request. Adding a network hop to every authorisation check is a real cost.

### 4.4 Distributed-system failure modes

Three hosts means three things that can fail independently. Network partitions become real. Time skew becomes real. Partial failure (AuthN responsive, AuthZ slow, User Info unreachable) becomes a state the system has to handle gracefully. A single host treating "the system is up" as a binary is no longer accurate.

The system needs:

- Health checks per service.
- Timeout and retry policies for cross-service calls.
- Circuit breakers to prevent cascading failure.
- Default-deny behaviour on AuthZ unavailability (already required by the SOC 2 doc).
- Time synchronisation (NTP or chrony) on every host.

None of these are exotic; all of them are work.

### 4.5 Deployment complexity

A single host with one container has one deployment pipeline. Three hosts with three containers have three pipelines, three rollback procedures, three sets of canary metrics, three smoke-test suites. The release of a feature that touches all three services becomes a coordinated multi-service release, with sequencing concerns (deploy AuthZ first to add a permission, then User Info to consume it, then AuthN if relevant).

This is the cost of independent deployability: independent deployability is also independent coordination.

### 4.6 Observability complexity

Distributed tracing becomes mandatory rather than optional. A request that traverses Client to AuthN to AuthZ to User Info has four spans across three hosts; without tracing, debugging a slow request is guesswork. Tracing infrastructure (OpenTelemetry collectors, a tracing back-end like Jaeger or Tempo, instrumentation in every service) is itself another set of components to run.

Log correlation across hosts requires a request ID propagated through every call and present in every log line. Without it, correlating events across the three hosts during an incident is painful.

### 4.7 Disaster recovery is three plans, not one

Each host needs its own backup, its own restore procedure, its own DR test cadence. Cross-service consistency during DR (does the restored AuthZ database match the restored User Info database?) is a question that needs an answer.

### 4.8 Cost of single-host failure is concentrated

If the AuthN host goes down, the entire platform is down: nobody can log in. With co-located redundancy on one larger machine, hardware failure takes everything down at once but at least there is only one thing to recover. With three hosts, there are three independent single points of failure unless each one is itself redundant. Achieving real availability requires multi-host, multi-zone redundancy per service, which multiplies host count by 2 or 3 again.

The naïve one-host-per-service topology gives the worst of both worlds: distributed-system complexity without distributed-system availability. Realistic deployments need at least two hosts per service in different availability zones, behind a load balancer.

### 4.9 Database co-location is a separate decision

The diagram shows each service's database on the same host as the service. That is a simplification. In production:

- Co-located databases share fate with the service. A host failure takes both down.
- Separate database hosts add three more hosts (or six, with replicas) and another network hop.
- Managed database services (RDS, Cloud SQL) externalise the problem at the cost of vendor dependency.

The deployment team has to decide. The proposal as stated does not.

---

## 5. Alternatives

The proposal is one point on a spectrum. Five alternatives cover most of the spectrum.

### 5.1 Alternative A: Three services, three pods on a shared Kubernetes cluster

```
              +-------------------- Kubernetes cluster ---------------------+
              |                                                              |
              |   +-----------+   +-----------+   +-----------+              |
              |   |  AuthN    |   |  AuthZ    |   | User Info |              |
              |   |  pod      |   |  pod      |   |  pod      |              |
              |   +-----------+   +-----------+   +-----------+              |
              |                                                              |
              |   Each pod scheduled across multiple worker nodes.            |
              |   Network policies enforce isolation between pods.            |
              |   Service mesh (mTLS, retries, observability).                |
              +--------------------------------------------------------------+
```

**Positives:** Single platform to operate. Native horizontal scaling. Well-understood failure recovery. Service mesh handles mTLS, retries, and observability uniformly. Compliance separation is logical (network policies, RBAC, namespaces) rather than physical, which most assessors accept.

**Negatives:** Shared kernel means a node-level escape can affect any pod scheduled there. The compliance argument needs additional controls (taint/toleration to keep AuthN pods on dedicated nodes, for example). Kubernetes is itself a substantial operational surface; small teams sometimes underestimate the cost.

**When to choose:** Most deployments at moderate scale. The default for organisations already running Kubernetes for other workloads.

### 5.2 Alternative B: Three services, three Kubernetes clusters

A stronger version of A: each service on a separate cluster, in a separate cloud account or subscription if possible.

**Positives:** Stronger blast-radius bound than A. Cluster-level compromise stops at the cluster boundary. Compliance audit boundaries match cluster boundaries. The "physical" separation that the original proposal achieves with three hosts, achieved through three control planes.

**Negatives:** Three clusters cost more than one, both in infrastructure and in operational complexity. Cross-cluster service discovery, cross-cluster mTLS, and cross-cluster observability are all extra work.

**When to choose:** High-compliance environments where regulators want to see independent control planes. Multi-tenant SaaS platforms where the trust boundary matters at infrastructure level.

### 5.3 Alternative C: Three services, three containers on one host

```
                +------------------- Single host -------------------+
                |                                                    |
                |   +-----------+   +-----------+   +-----------+   |
                |   |  AuthN    |   |  AuthZ    |   | User Info |   |
                |   |  container|   |  container|   |  container|   |
                |   +-----------+   +-----------+   +-----------+   |
                |                                                    |
                |          Single Docker daemon, single kernel       |
                +----------------------------------------------------+
```

**Positives:** Cheapest to operate. Smallest infrastructure footprint. Inter-service calls are loopback, minimum latency. Single host to patch, monitor, back up.

**Negatives:** All three services share a kernel; container escape compromises all three. Compliance argument is weaker; some assessors will not accept it for high-sensitivity data. Single point of hardware failure takes the whole platform down. Services compete for CPU, memory, disk I/O.

**When to choose:** Development environments. Demonstration environments. Very small internal deployments where the real risk is operational complexity rather than compromise.

### 5.4 Alternative D: Modular monolith with logical separation

A single deployable artefact (one binary, one container, one host) that contains the AuthN, AuthZ, and User Info modules as internal sub-systems. Module boundaries are enforced at the language level (separate packages, no cross-module imports of internal types) rather than at the network level. Each module owns its own database schema; the shared process holds three database connections.

**Positives:** Simplest possible deployment. No cross-module network latency. Refactoring across module boundaries is fast (compiler catches violations). Suitable for a team that wants the architectural separation without the operational distributed-system tax.

**Negatives:** Process-level compromise compromises all three modules. No physical compliance boundary. Independent scaling and independent technology choice are both lost. The team has to enforce module separation through discipline and tooling rather than network policy.

**When to choose:** Early-stage products where the team is small and the requirement for separation is logical (clean code, future flexibility) rather than regulatory (provable isolation). The modular monolith can be split into three services later if the requirement changes; the API contracts in this folder are designed to make that split low-cost.

### 5.5 Alternative E: Serverless functions per service

Each service implemented as a set of cloud functions (Lambda, Cloud Functions, Azure Functions). Scaling is automatic and per-request. There are no servers to manage at all.

**Positives:** Zero host-level operational overhead. Per-request scaling matches AuthN's burstiness particularly well. Compliance argument is held by the cloud provider for the substrate, by the service for the data plane.

**Negatives:** Cold starts add latency to AuthZ on the hot path; for sub-10ms decision SLOs, this can be disqualifying. Vendor lock-in is significant. Long-running connections (database connection pools, JWKS caches) are hard to maintain across short-lived function invocations. Cost model is per-invocation; for high-volume AuthZ, this can be more expensive than reserved capacity.

**When to choose:** Low-volume internal deployments where cold-start latency is acceptable. Greenfield deployments already committed to a serverless platform. Bursty workloads where steady-state load is far below peak.

---

## 6. Comparison summary

| Concern | Proposal (3 hosts) | A: Shared cluster | B: Three clusters | C: One host | D: Monolith | E: Serverless |
|---------|:------------------:|:-----------------:|:-----------------:|:-----------:|:-----------:|:-------------:|
| Blast radius bound | Strongest | Moderate | Strong | Weak | Weakest | Provider-bounded |
| Compliance separability | Strongest | Logical | Strong | Weak | Weakest | Mixed |
| Inter-service latency | High | Moderate | Moderate | Low | Lowest | Variable (cold starts) |
| Infrastructure cost | High | Moderate | High | Low | Lowest | Pay-per-use |
| Operational complexity | High | Moderate | Highest | Lowest | Low | Low (vendor-managed) |
| Independent scaling | Yes | Yes | Yes | Constrained | No | Yes |
| Independent tech stack | Yes | Yes | Yes | Yes | No | Constrained by platform |
| Suitability for AuthZ hot path | Acceptable | Good | Good | Best | Best | Risky (cold starts) |
| Team size needed | Medium-Large | Medium | Large | Small | Small | Small |

---

## 7. Recommendation

The one-Docker-instance-per-host proposal is the right *default* for a deployment that:

- Has a compliance regime that benefits from a physically demonstrable boundary (PCI-equivalent for credentials, GDPR for personal data, SOC 2 for access decisions, all at once).
- Has the operational maturity to run multi-host distributed systems (tracing, monitoring, mTLS, secrets management).
- Has a workload large enough that the per-host overhead is amortised across real traffic.

It is the wrong default for a deployment that:

- Is small, internal, or experimental, where Alternative C or D is more appropriate.
- Is large enough to need horizontal scaling per service, where Alternative A is more appropriate.
- Is bursty in a way that benefits from per-request scaling, where Alternative E may be appropriate.

For most production deployments at moderate scale, **Alternative A (three pods on a shared Kubernetes cluster, with namespace and network policy isolation, and selective node taints to keep AuthN on dedicated nodes)** offers the best ratio of isolation to operational cost. It preserves the architecture document's separation arguments at the network level, and it inherits the cluster's redundancy story without forcing the team to build it from primitives.

The original proposal is preferable to Alternative A only when the compliance regime is strict enough that "logical separation enforced by Kubernetes" is not an acceptable answer.

---

## 8. Decisions deferred

The following decisions are not made by this document and require their own analysis before deployment:

- **Database co-location.** Co-located on the service host, paired host, or managed service.
- **Multi-zone redundancy.** Two or more replicas per service across availability zones, behind a load balancer.
- **Message bus topology.** Same hosts, dedicated bus host, or managed bus service.
- **Secret distribution.** Per-host KMS, central KMS with per-service IAM, or HashiCorp Vault.
- **Service mesh choice.** Istio, Linkerd, Cilium, Consul, or no mesh at all.
- **CI/CD pipeline.** Shared platform pipeline, per-service pipeline, or hybrid.

Each of these is a design decision in its own right and warrants its own document if the answer is not obvious from organisational defaults.

---

## 9. Open questions

1. What latency budget does the consuming platform have for AuthZ decisions? If sub-5ms p99, several alternatives become unviable.
2. What is the regulatory environment? PCI level, GDPR-only, SOC 2-only, all three?
3. What is the existing infrastructure stack? Kubernetes-shop, VM-shop, serverless-shop, mixed?
4. What is the team size and operational maturity? Three hosts to operate is more work than three pods on an existing cluster.
5. What is the expected steady-state traffic? At low traffic, Alternative C or E dominates; at high traffic, Alternative A or B dominates.

The team owning the decision should answer these before treating the proposal as accepted.

---

## 10. Conformance

If the proposal is accepted, conformance with the spec set in this folder requires that:

1. Each of the three services runs in exactly one production container per host.
2. The three hosts have no shared filesystem, no shared database, and no shared network namespace.
3. Inter-service communication uses mTLS with the per-host service certificate.
4. Each host publishes its own metrics, logs, and traces with the service identity tagged at source.
5. The conformance checklist in `auth-separation_README_v1.md` (10 items) is met.
6. The conformance checklists in the three compliance docs are met.

If the proposal is rejected in favour of an alternative, the alternative must still satisfy items 5 and 6. Topology is a means to those ends, not an end in itself.
