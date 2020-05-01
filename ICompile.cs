namespace CSScript
{
    /// <summary>
    /// Интерфейс для взаимодействия со скомпилированным скриптом
    /// </summary>
    public interface ICompile
    {
        int Execute(string arg);
    }
}
