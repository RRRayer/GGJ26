public abstract class FactorySO<T> : DescriptionSO, IFactory<T>
{
    public abstract T Create();
}