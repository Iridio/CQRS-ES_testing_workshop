using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace CqrsMovie.Muflone.EventStore.Persistence
{
  public interface IEventStorePositionRepository
  {
    Task<Position> GetLastPosition();
    Task Save(long commitPosition, long preparePosition);
  }
}
