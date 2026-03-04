using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerElimination))]
public class DeathmatchMeleeAttack : NetworkBehaviour
{
    [SerializeField] private float attackRange = 2.2f;
    [SerializeField] private float attackRadius = 0.45f;
    [SerializeField] private float attackCooldown = 0.6f;
    [SerializeField] private LayerMask attackMask = -1;
    [SerializeField, Range(-1f, 1f)] private float frontDotThreshold = 0.1f;
    [SerializeField] private bool enableDebugLogs = true;

    private PlayerElimination elimination;
    private float nextAttackTime;
    private PlayerInput playerInput;

    private void Awake()
    {
        elimination = GetComponent<PlayerElimination>();
        playerInput = GetComponent<PlayerInput>();
    }

    private void Update()
    {
        if (Object == null || Object.HasInputAuthority == false)
        {
            return;
        }

        if (GameModeRuntime.IsDeathmatch == false)
        {
            return;
        }

        if (elimination != null && elimination.Object != null && elimination.Object.IsValid && elimination.IsEliminated)
        {
            return;
        }

        if (WasAttackPressedThisFrame() == false)
        {
            return;
        }

        if (Time.time < nextAttackTime)
        {
            return;
        }

        nextAttackTime = Time.time + Mathf.Max(0.05f, attackCooldown);

        if (TryBuildAim(out Vector3 origin, out Vector3 direction) == false)
        {
            return;
        }

        if (Object.HasStateAuthority)
        {
            ExecuteAttack(origin, direction);
        }
        else
        {
            RpcRequestMeleeAttack(origin, direction);
        }

        if (enableDebugLogs)
        {
            Debug.Log($"[Deathmatch] Melee input accepted: player={Object.InputAuthority.RawEncoded}, hasStateAuthority={Object.HasStateAuthority}");
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RpcRequestMeleeAttack(Vector3 origin, Vector3 direction)
    {
        ExecuteAttack(origin, direction);
    }

    private void ExecuteAttack(Vector3 origin, Vector3 direction)
    {
        var match = FindFirstObjectByType<DeathmatchMatchController>();
        if (match == null || match.IsEnabled == false)
        {
            if (enableDebugLogs)
            {
                Debug.LogWarning("[Deathmatch] Melee ignored: DeathmatchMatchController missing or disabled.");
            }
            return;
        }

        int localRaw = Object != null ? Object.InputAuthority.RawEncoded : 0;
        if (TryFindPlayerTarget(origin, direction, localRaw, out int victimRaw))
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[Deathmatch] Melee player hit: attacker={localRaw} victim={victimRaw}");
            }

            match.RpcRegisterPlayerKill(localRaw, victimRaw);
            return;
        }

        if (TryFindNpcTarget(origin, direction, out NPCController npc))
        {
            if (enableDebugLogs)
            {
                Debug.Log($"[Deathmatch] Melee NPC hit: attacker={localRaw}");
            }

            if (npc != null && npc.IsDead == false)
            {
                npc.RpcTriggerDead();
            }

            match.RpcRegisterNpcPenalty(localRaw);
            return;
        }

        if (enableDebugLogs)
        {
            Debug.Log($"[Deathmatch] Melee miss: attacker={localRaw}, origin={origin}, direction={direction}");
        }
    }

    private bool TryFindPlayerTarget(Vector3 origin, Vector3 direction, int selfRaw, out int victimRaw)
    {
        victimRaw = 0;
        var hits = Physics.OverlapSphere(origin, attackRange, attackMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        float bestDistance = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (col == null)
            {
                continue;
            }

            var role = col.GetComponentInParent<PlayerRole>();
            if (role == null || role.Object == null)
            {
                continue;
            }

            int raw = role.Object.InputAuthority.RawEncoded;
            if (raw == selfRaw)
            {
                continue;
            }

            var targetElimination = role.GetComponent<PlayerElimination>();
            if (targetElimination != null && targetElimination.Object != null && targetElimination.Object.IsValid && targetElimination.IsEliminated)
            {
                continue;
            }

            Vector3 toTarget = role.transform.position - origin;
            float distance = toTarget.magnitude;
            if (distance < 0.05f)
            {
                continue;
            }

            float dot = Vector3.Dot(direction.normalized, toTarget / distance);
            if (dot < frontDotThreshold)
            {
                continue;
            }

            if (distance < bestDistance)
            {
                bestDistance = distance;
                victimRaw = raw;
            }
        }

        return victimRaw != 0;
    }

    private bool TryFindNpcTarget(Vector3 origin, Vector3 direction, out NPCController targetNpc)
    {
        targetNpc = null;
        var hits = Physics.OverlapSphere(origin, attackRange, attackMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0)
        {
            return false;
        }

        float bestDistance = float.MaxValue;
        for (int i = 0; i < hits.Length; i++)
        {
            var col = hits[i];
            if (col == null)
            {
                continue;
            }

            var npc = col.GetComponentInParent<NPCController>();
            if (npc == null || npc.IsDead)
            {
                continue;
            }

            Vector3 toTarget = npc.transform.position - origin;
            float distance = toTarget.magnitude;
            if (distance < 0.05f)
            {
                continue;
            }

            float dot = Vector3.Dot(direction.normalized, toTarget / distance);
            if (dot < frontDotThreshold)
            {
                continue;
            }

            if (distance < bestDistance)
            {
                bestDistance = distance;
                targetNpc = npc;
            }
        }

        return targetNpc != null;
    }

    private bool TryBuildAim(out Vector3 origin, out Vector3 direction)
    {
        origin = transform.position + Vector3.up * 1.2f;
        direction = transform.forward;

        Camera cam = Camera.main;
        if (cam == null)
        {
            return true;
        }

        direction = cam.transform.forward;
        return true;
    }

    private bool WasAttackPressedThisFrame()
    {
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            return true;
        }

        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }

        if (playerInput == null || playerInput.actions == null)
        {
            return false;
        }

        var fire = playerInput.actions.FindAction("Fire", false);
        if (fire != null && fire.WasPressedThisFrame())
        {
            return true;
        }

        var shoot = playerInput.actions.FindAction("Shoot", false);
        if (shoot != null && shoot.WasPressedThisFrame())
        {
            return true;
        }

        return false;
    }
}
