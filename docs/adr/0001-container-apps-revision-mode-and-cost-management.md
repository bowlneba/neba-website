# ADR-0001: Container Apps Revision Mode and Cost Management

## Status

Proposed

## Context

Azure Container Apps creates a new revision on each deployment. The platform supports two revision modes:

1. **Single** (default): Only one revision is active at a time. When a new deployment occurs, the platform routes 100% of traffic to the new revision and typically deactivates the previous revision (scales it to 0 replicas).

2. **Multiple**: Multiple revisions can run simultaneously, allowing traffic splitting for blue/green deployments, canary releases, or A/B testing.

### Current Deployment Behavior

- Our `azure.yaml` specifies `host: containerapp` for the web service
- `azd up` creates a new container image and deploys a new revision
- Each revision can have its own replica scaling configuration (`minReplicas`, `maxReplicas`)
- Revisions are immutable — code changes require deploying a new revision

### Cost Implications

Azure Container Apps billing is based on:

- **vCPU-seconds** and **GB-seconds** for each running replica
- **Log Analytics/Application Insights** ingestion and retention
- **Azure Container Registry** storage
- **Network egress** for outbound traffic

Key cost factors:

- Each running replica consumes billable compute resources
- Setting `minReplicas > 0` guarantees at least that many replicas are always running and billing
- Multiple active revisions with `minReplicas > 0` multiply the minimum compute cost
- Idle replicas still incur costs even when receiving no traffic

### Observed Behavior

Current production deployment (`rg-bowlneba-prod`):

```json
{
  "activeRevisionsMode": "Single",
  "template": {
    "scale": {
      "minReplicas": 1,
      "maxReplicas": 10
    }
  }
}
```

With this configuration:

- New deployments create a new revision with `minReplicas=1`
- Previous revision is deactivated (scaled to 0) automatically
- Cost remains stable across deployments (always 1-10 replicas total)

## Decision

For production deployments, we will:

1. **Use Single mode by default** for straightforward deployments where we want immediate cutover to the new version
   - Keeps operational complexity low
   - Prevents cost inflation from multiple running revisions
   - Appropriate for most standard deployments

2. **Use Multiple mode only when needed** for:
   - Blue/green deployments requiring validation before traffic shift
   - Canary releases (gradual traffic migration)
   - A/B testing requiring traffic splitting
   - When these scenarios are needed, explicitly document the traffic splitting strategy and rollback plan

3. **Set `minReplicas=0` for non-production environments** to minimize idle costs when traffic is low or intermittent

4. **Review replica scaling settings per environment**:
   - **Production**: `minReplicas=1` (or higher based on availability requirements), `maxReplicas` based on load testing
   - **Staging/QA**: `minReplicas=0-1`, `maxReplicas` lower than production
   - **Development**: `minReplicas=0`, `maxReplicas=3`

5. **Document any revision mode changes** in deployment runbooks when transitioning to Multiple mode

## Consequences

### Positive

- **Predictable costs**: Single mode with appropriate `minReplicas` prevents surprise costs from abandoned revisions
- **Simple operations**: Default behavior (new revision → 100% traffic) is straightforward
- **Flexibility available**: Can switch to Multiple mode when advanced deployment patterns are needed
- **Scale to zero in dev**: Setting `minReplicas=0` in non-prod saves costs during off-hours

### Negative

- **No instant rollback**: Single mode deactivates old revisions; rollback requires redeploying the previous image or reactivating a previous revision manually
- **No traffic splitting by default**: A/B testing or canary deploys require switching to Multiple mode first
- **Cold start latency**: `minReplicas=0` introduces cold start delays (typically 3-10 seconds)

### Mitigation Strategies

- **Rollback process**: Document how to reactivate a previous revision or redeploy a previous container image tag
- **Canary deployment preparation**: Create deployment playbook for switching to Multiple mode, setting traffic weights, and monitoring metrics before completing cutover
- **Cost monitoring**: Set up Azure Cost Management alerts and dashboard to track Container Apps spend by resource group and environment

## Implementation Notes

### Check current revision mode

```bash
az containerapp show -g <resource-group> -n <app-name> \
  --query properties.configuration.activeRevisionsMode -o tsv
```

### List revisions and traffic weights

```bash
az containerapp revision list -g <resource-group> -n <app-name> -o table
```

### Change to Multiple mode (when needed)

```bash
az containerapp revision set-mode \
  --resource-group <resource-group> \
  --name <app-name> \
  --mode Multiple
```

### Split traffic between revisions

```bash
az containerapp revision set-traffic \
  --resource-group <resource-group> \
  --name <app-name> \
  --traffic <revision-name>=30 latestRevision=70
```

### Update replica scaling

```bash
az containerapp update \
  -g <resource-group> \
  -n <app-name> \
  --set properties.template.scale.minReplicas=0 \
        properties.template.scale.maxReplicas=10
```

### Access specific revision (for testing)

Each revision has its own FQDN:

```bash
# Get revision FQDNs
az containerapp revision list -g <rg> -n <app> \
  --query '[].{name:name, fqdn:properties.fqdn}' -o table

# Test a specific revision directly (bypasses traffic weights)
curl https://<revision-fqdn>/health
```

## References

- [Azure Container Apps revisions documentation](https://learn.microsoft.com/azure/container-apps/revisions)
- [Azure Container Apps pricing](https://azure.microsoft.com/pricing/details/container-apps/)
- [Traffic splitting strategies](https://learn.microsoft.com/azure/container-apps/revisions-manage)

## Related Decisions

None yet. Future ADRs may reference this when discussing:

- Deployment strategies (blue/green, canary)
- Environment-specific configurations
- Cost optimization approaches
