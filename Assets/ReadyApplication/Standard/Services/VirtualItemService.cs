using System.Collections.Generic;
using System.Threading;
using ReadyApplication.Core;
using RGN.Modules.VirtualItems;

namespace ReadyApplication.Standard
{
    public interface IVirtualItemService
    {
	    RepositoryEntityCache<string, VirtualItem> CachedVirtualItems { get; }
	    event System.Action<VirtualItem> CachedVirtualItemUpdated;
	    event System.Action CachedVirtualItemsUpdated;
		IFluentAction<List<VirtualItem>> GetByIds(string id, CancellationToken cancellationToken = default);
        IFluentAction<List<VirtualItem>> GetByIds(List<string> ids, CancellationToken cancellationToken = default);
        IFluentAction<List<VirtualItem>> GetByTags(string tag, CancellationToken cancellationToken = default);
        IFluentAction<List<VirtualItem>> GetByTags(List<string> tags, CancellationToken cancellationToken = default);
        IFluentAction<List<VirtualItem>> GetForThisApp(int limit, string startAfter = "", CancellationToken cancellationToken = default);
    }
    
    public class VirtualItemService : IVirtualItemService
    {
        private readonly IVirtualItemRepository _virtualItemRepository;

        public RepositoryEntityCache<string, VirtualItem> CachedVirtualItems => _virtualItemRepository.CachedVirtualItems;

        public event System.Action<VirtualItem> CachedVirtualItemUpdated
		{
			add => _virtualItemRepository.CachedVirtualItemUpdated += value;
			remove => _virtualItemRepository.CachedVirtualItemUpdated -= value;
		}
        public event System.Action CachedVirtualItemsUpdated
        {
            add => _virtualItemRepository.CachedVirtualItemsUpdated += value;
            remove => _virtualItemRepository.CachedVirtualItemsUpdated -= value;
        }

        public VirtualItemService(IVirtualItemRepository virtualItemRepository)
        {
            _virtualItemRepository = virtualItemRepository;
        }

        public IFluentAction<List<VirtualItem>> GetByIds(string id, CancellationToken cancellationToken = default)
			=> _virtualItemRepository.GetByIds(id, cancellationToken);

		public IFluentAction<List<VirtualItem>> GetByIds(List<string> ids, CancellationToken cancellationToken = default)
            => _virtualItemRepository.GetByIds(ids, cancellationToken);

        public IFluentAction<List<VirtualItem>> GetByTags(string tag, CancellationToken cancellationToken = default)
			=> _virtualItemRepository.GetByTags(tag, cancellationToken);

		public IFluentAction<List<VirtualItem>> GetByTags(List<string> tags, CancellationToken cancellationToken = default)
            => _virtualItemRepository.GetByTags(tags, cancellationToken);

        public IFluentAction<List<VirtualItem>> GetForThisApp(int limit, string startAfter = "", CancellationToken cancellationToken = default)
            => _virtualItemRepository.GetForThisApp(limit, startAfter, cancellationToken);
    }
}