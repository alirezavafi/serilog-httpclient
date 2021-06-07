using System;
using System.Collections.Generic;

namespace Serilog.HttpClient
{
    /// <summary>
    /// An aggregated disposable object for collection of disposable objects 
    /// </summary>
    public class AggregatedDisposable : IDisposable
    {
        private readonly IEnumerable<IDisposable> _disposables;

        /// <summary>
        /// disposable objects to unify as single disposable
        /// </summary>
        /// <param name="disposables"></param>
        public AggregatedDisposable(IEnumerable<IDisposable> disposables)
        {
            _disposables = disposables;
        }
        
        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            foreach (var disposable in _disposables)
            {
                disposable.Dispose();
            }
        }
    }
}