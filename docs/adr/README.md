# Architecture Decision Records (ADRs)

This directory contains Architecture Decision Records (ADRs) that document significant architectural and infrastructure decisions for the NEBA Website project.

## What is an ADR?

An ADR is a document that captures an important architectural decision along with its context and consequences. ADRs help teams:

- Understand why decisions were made
- Avoid revisiting settled questions
- Onboard new team members
- Review past decisions when circumstances change

## When to Create an ADR

Create an ADR when making decisions about:

- Technology choices (frameworks, databases, cloud services)
- Architectural patterns and structures
- Infrastructure and deployment approaches
- Security and compliance strategies
- Performance optimization strategies
- Cost management approaches
- Development workflow and tooling

## ADR Format

Each ADR follows this structure:

1. **Title**: Brief, descriptive name
2. **Status**: Proposed | Accepted | Deprecated | Superseded
3. **Context**: Background, problem statement, and constraints
4. **Decision**: What was decided and why
5. **Consequences**: Positive and negative outcomes, trade-offs, and mitigation strategies

## Naming Convention

ADRs are numbered sequentially:

```list
0001-container-apps-revision-mode-and-cost-management.md
0002-next-decision.md
0003-another-decision.md
```

## Creating a New ADR

1. Determine the next number in sequence
2. Create a new file: `NNNN-brief-title.md`
3. Use the template structure above
4. Start with status "Proposed"
5. Update to "Accepted" after team review/approval
6. Reference related ADRs when relevant

## ADR Lifecycle

- **Proposed**: Under discussion
- **Accepted**: Approved and implemented
- **Deprecated**: No longer valid but kept for reference
- **Superseded**: Replaced by a newer ADR (reference the new one)

## Current ADRs

- [ADR-0001](0001-container-apps-revision-mode-and-cost-management.md): Container Apps Revision Mode and Cost Management
- [ADR-0002](0002-azure-key-vault-for-secrets-management.md): Azure Key Vault for Secrets Management

## Resources

- [Documenting Architecture Decisions](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions) by Michael Nygard
- [ADR GitHub Organization](https://adr.github.io/)
