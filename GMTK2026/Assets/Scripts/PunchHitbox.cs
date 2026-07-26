using UnityEngine;

public class PunchHitbox : MonoBehaviour
{
    private PunchComboAbility parentAbility;

    private void Awake()
    {
        parentAbility = GetComponentInParent<PunchComboAbility>();
        if (parentAbility == null)
            Debug.LogError("PunchHitbox must be child of object with PunchComboAbility");
    }

    private void OnTriggerEnter(Collider other)
    {
        //if (parentAbility == null) return;
        //if (!parentAbility.IsPunching) return;
        //parentAbility.OnHit(other);
    }
}