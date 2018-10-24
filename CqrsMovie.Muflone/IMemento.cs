using System;

namespace CqrsMovie.Muflone
{
  public interface IMemento
  {
    Guid Id { get; set; }
    int Version { get; set; }
  }
}