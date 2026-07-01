---
name: codex-plan-review
description: >
  [TMUX MODE] Send a non-trivial implementation plan to Codex for an
  independent architectural review before implementation.
  This skill performs review only.
  It never modifies files or implements the plan.
---

# Codex Plan Review Skill

Review an implementation plan with Codex before implementation begins.

This skill exists to provide an **independent second opinion**.

The objective is **not** to improve the plan automatically.

The objective is to identify:

- architectural risks
- missing edge cases
- unnecessary complexity
- hidden assumptions
- testing gaps
- deployment risks

The primary agent remains responsible for integrating the feedback.

---

# Review Philosophy

The reviewer must behave like an experienced senior software architect.

It must **challenge** the proposed solution.

It should attempt to answer:

- What is wrong?
- What is missing?
- What is risky?
- What is over-engineered?
- Can this become simpler?

The reviewer should **not** attempt to defend the plan.

---

# When To Use

Use this skill when one or more of the following are true:

- User explicitly requests `/codex-plan-review`
- User asks to review a plan with Codex
- Change affects multiple projects
- Change affects more than approximately five files
- Framework architecture changes
- Public API changes
- Authentication or authorization changes
- ASP.NET Core middleware changes
- Reverse proxy changes
- Blazor rendering/state management
- SignalR
- SQL schema
- Dapper data access
- Transactions
- Performance work
- Concurrency
- Async code
- Deployment
- Build system
- MSBuild
- NuGet packages
- CI/CD

Do **not** use this skill for:

- typos
- logging
- formatting
- local refactoring
- obvious one-file fixes
- trivial bug fixes

---

# Step 0 - Verify tmux

Run

```bash
[ -n "$TMUX" ] && echo TMUX_OK || echo NOT_IN_TMUX
```

If tmux is unavailable:

Stop immediately.

Explain that this skill requires tmux.

Do not continue.

---

# Step 1 - Locate Project

Determine project root.

Prefer

```bash
git rev-parse --show-toplevel
```

Locate

```
.agent-collab
```

If it cannot be located:

Stop.

Ask user to initialize the collaboration directory.

Do not continue.

Ensure

```
requests/
responses/
```

exist.

---

# Step 2 - Verify No Existing Review

Read

```
.agent-collab/status
```

If status is

```
pending
```

stop immediately.

Do not overwrite an existing request.

---

# Step 3 - Gather Plan

The plan should contain

- Goal
- Motivation
- Overall approach
- Files expected to change
- Step-by-step implementation
- Risks
- Build commands
- Test commands

If no plan exists:

Create one first.

Then continue.

---

# Step 4 - Create Review Request

Write

```
.agent-collab/requests/task.md
```

using an atomic write.

Use this structure.

# Task Type

PLAN_REVIEW

# Objective

Review only.

Do NOT implement.

Do NOT modify files.

Do NOT generate patches.

Do NOT execute destructive commands.

# Project Root

...

# Plan

...

# Constraints

- Prefer existing repository conventions.
- Prefer smallest possible change.
- Do not introduce packages unless justified.
- Avoid unnecessary abstractions.
- Respect existing architecture.
- If context is missing, explicitly say so.

# Files To Read

Absolute paths only.

Never include

- secrets
- certificates
- production credentials
- private configuration
- customer data

# Review Checklist

Evaluate

## Architecture

- Is the design appropriate?
- Is it too complex?
- Is there a simpler approach?

## Correctness

- Missing requirements
- Hidden assumptions
- Edge cases

## .NET

- ASP.NET Core
- Middleware order
- Dependency Injection
- Authentication
- Authorization
- Cookies
- Sessions
- SignalR
- Hosted services

## Blazor

- Render lifecycle
- State management
- Disposal
- JS interop
- Circuit lifetime

## SQL

- Transactions
- Parameterization
- Dapper usage
- Concurrency
- Locking
- Schema assumptions

## Performance

- Async correctness
- Cancellation
- Thread safety
- Allocations
- Resource disposal

## Deployment

- Reverse proxy
- PathBase
- HTTPS
- Configuration
- Windows Service
- Health checks

## Testing

- Unit tests
- Integration tests
- Manual verification
- Runtime validation

# Required Output

Return

## Verdict

APPROVE

APPROVE_WITH_CHANGES

DO_NOT_IMPLEMENT_YET

## Blocking Issues

...

## Important Issues

...

## Optional Suggestions

...

## Simpler Alternative

...

## Required Plan Revisions

...

## Verification Checklist

...

---

# Step 5 - Mark Pending

Write

```
pending
```

to

```
.agent-collab/status
```

---

# Step 6 - Trigger Codex

Use

```
CODEX_TMUX_TARGET
```

instead of hardcoded pane numbers.

If target does not exist:

Stop.

Do not continue.

---

# Step 7 - Wait

Poll every three seconds.

Maximum timeout:

15 minutes.

If timeout expires:

Report timeout.

Do not implement.

---

# Step 8 - Read Response

Read

```
responses/response.md
```

Present the review grouped as

- Blockers
- Important
- Optional

Revise the plan using only

- Blockers
- Important findings

Ignore optional suggestions unless they clearly improve the design.

Present the revised plan.

Ask user whether implementation should begin.

Finally

```
status = idle
```

---

# Important Rules

Never modify the repository.

Never implement the reviewed plan.

Never suppress critical findings.

Prefer missing context over guessing.

Prefer simple architecture over clever architecture.

Prefer consistency with the existing repository over introducing new patterns.

Avoid review comments about style unless they affect correctness or maintainability.

Focus on correctness, architecture, testability, deployment and long-term maintainability.