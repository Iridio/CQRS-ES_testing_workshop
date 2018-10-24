using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CqrsMovie.Muflone.Messages.Commands;
using CqrsMovie.Muflone.Messages.Events;
using KellermanSoftware.CompareNetObjects;
using Xunit;
using Xunit.Sdk;

namespace CqrsMovie.Seats.Domain.Tests
{
  /// <summary>
  /// https://github.com/luizdamim/NEventStoreExample/tree/master/NEventStoreExample.Test
  /// </summary>
  /// <typeparam name="TCommand"></typeparam>
  public abstract class CommandSpecification<TCommand> where TCommand : Command
  {
    //li si imposta dal costruttore del test se dovesse servire un'eccezione o il mock del repository
    protected Exception ExpectedException { get; set; }
    protected InMemoryEventRepository Repository { get; set; }

    protected CommandSpecification()
    {
      //Use this or mock it from test
      Repository = new InMemoryEventRepository();
    }

    [Fact]
    public async Task SetUp()
    {
      Repository.ApplyGivenEvents(Given().ToList());
      var handler = OnHandler();
      try
      {
        await handler.Handle(When());
        var expected = Expect().ToList();
        var published = Repository.Events;
        CompareEvents(expected, published);
      }
      catch (AssertActualExpectedException) //If an Assert Exception throw it to the sky
      {
        throw;
      }
      catch (Exception exception)
      {
        Assert.Equal(exception.GetType(), ExpectedException.GetType());
        Assert.Equal(exception.Message, ExpectedException.Message);
      }
    }

    protected abstract IEnumerable<DomainEvent> Given();
    protected abstract IEnumerable<DomainEvent> Expect();
    protected abstract TCommand When();
    protected abstract ICommandHandler<TCommand> OnHandler();

    private static void CompareEvents(IEnumerable<DomainEvent> expected, IEnumerable<DomainEvent> published)
    {
      if (published == null)
        published = new List<DomainEvent>();

      Assert.True(expected.Count() == published.Count(), "Different number of expected/published events.");

      var config = new ComparisonConfig();
      config.MembersToIgnore.Add("Headers");
      config.MembersToIgnore.Add("MessageId");

      var compareObjects = new CompareLogic(config);
      var eventPairs = expected.Zip(published, (e, p) => new { Expected = e, Published = p });
      foreach (var eventPair in eventPairs)
      {
        var result = compareObjects.Compare(eventPair.Expected, eventPair.Published);
        Assert.True(result.AreEqual, $"Events {eventPair.Expected.GetType()} and {eventPair.Published.GetType()} are different: {result.DifferencesString}");
      }
    }
  }
}
