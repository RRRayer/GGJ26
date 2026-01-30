using System.Collections.Generic;
using UnityEngine;

public abstract class PoolSO<T> : DescriptionSO, IPool<T> 
{
    public abstract IFactory<T> Factory { get; } // 멤버 생성을 위한 팩토리
    
    protected Stack<T> available = new Stack<T>(); // 풀 멤버들 저장
    protected bool hasBeenPrewarmed; // 초기화 돼는지 확인

    /// <summary>
    /// 대기 스택이 비었을 때, Request가 들어오면 새로운 객체를 생성해 반환한다.
    /// </summary>
    protected virtual T Create()
    {
        return Factory.Create();
    }

    /// <summary>
    /// 풀을 초기화 한다.
    /// </summary>
    public virtual void Prewarm(int initialSize)
    {
        if (hasBeenPrewarmed) return;
        for (int i = 0; i < initialSize; ++i)
        {
            available.Push(Create());
        }
        hasBeenPrewarmed = true;
    }

    /// <summary>
    /// 대기 스택의 멤버 중 하나를 깨우거나 생성해 반환한다.
    /// </summary>
    public virtual T Request()
    {
       return available.Count > 0 ? available.Pop() : Create();
    }

    /// <summary>
    /// 생명 주기가 끝난 멤버 오브젝트는 다시 대기 스택에 들어간다.
    /// </summary>
    public virtual void Return(T member)
    {
        available.Push(member);
    }

    /// <summary>
    /// 비활성화 시 초기화
    /// </summary>
    protected virtual void OnDisable()
    {
        available.Clear();
        hasBeenPrewarmed = false;
    }
}
