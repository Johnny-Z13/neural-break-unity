namespace Z13.Core
{
    /// <summary>
    /// Interface for components that require controlled initialization order.
    /// Implemented by singletons in the Boot scene that need explicit initialization
    /// rather than relying on Unity's non-deterministic Awake() order.
    /// </summary>
    public interface IBootable
    {
        /// <summary>
        /// Called by BootManager in a defined order. Previous singletons in the
        /// boot list are guaranteed to be initialized before this method is called.
        /// Use this instead of Awake() for initialization that depends on other singletons.
        /// </summary>
        void Initialize();
    }
}
