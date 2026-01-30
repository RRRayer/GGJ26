using UnityEngine;

public class ComponentFactory : FactorySO<Component>
{
    [SerializeField] private Component prefab;
    public override Component Create()
    {
        return Instantiate(prefab);
    }
}
