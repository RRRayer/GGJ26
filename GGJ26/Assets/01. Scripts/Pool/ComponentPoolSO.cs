using UnityEngine;

public abstract class ComponentPoolSO<T> : PoolSO<T> where T : Component
{
    private Transform parent;
    private Transform poolRoot;
    private Transform PoolRoot
    {
        get
        {
            if (poolRoot == null)
            {
                poolRoot = new GameObject(name).transform;
                poolRoot.SetParent(parent);
            }
            return poolRoot;
        }
    }
    
    /// <summary>
    /// 풀의 부모 트랜스폼을 지정한다.
    /// 생성/반환된 멤버들의 부모로 설정될 트랜스폼이다.
    /// </summary>
    public void SetParent(Transform p)
    {
        parent = p;
        PoolRoot.SetParent(parent);
    }

    public override T Request()
    {
        T member = base.Request();
        member.gameObject.SetActive(true);
        return member;
    }

    public override void Return(T member)
    {
        member.transform.SetParent(PoolRoot);
        member.gameObject.SetActive(false);
        base.Return(member);
    }

    protected override T Create()
    {
        T newMember = base.Create();
        newMember.transform.SetParent(PoolRoot);
        newMember.gameObject.SetActive(false);
        return newMember;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        if (poolRoot != null)
        {
            Destroy(poolRoot.gameObject);
        }
    }
}
