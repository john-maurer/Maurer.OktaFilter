using System.Runtime.CompilerServices;

namespace UnitTesting.Fixture
{
    /// <summary>
    /// Abstract base fixture that defines the framework of the encapsulation of an entity under test
    /// </summary>

    public abstract class AbstractFixture : IDisposable
    {
        /// <summary>
        /// Indicates whether or not concrete implementations have been disposed.
        /// </summary>

        private bool _disposed;

        /// <summary>
        /// Finalizer/Deconstructor
        /// </summary>

        ~AbstractFixture() => Dispose(false);

        /// <summary>
        /// Interface for defining setups and inputs to process unit under test
        /// </summary>
        /// <param name="parameters"></param>

        protected abstract void Arrange(params object[] parameters);

        /// <summary>
        /// Clean-up implementation
        /// </summary>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void CleanUp() { }

        /// <summary>
        /// Invokes underlying 'CleanUp' implementation
        /// </summary>
        /// <param name="disposing">Whether or not to dispose</param>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
                if (disposing)
                    CleanUp();

            _disposed = true;
        }

        /// <summary>
        /// Default constructor responsible for triggering the Arrange step definined in concrete implementations
        /// </summary>

        public AbstractFixture() => Arrange();

        /// <summary>
        /// Disposes of class if not already disposed
        /// </summary>

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
