﻿using System;
using System.Threading.Tasks;
using CqrsMovie.Messages.Commands.Seat;
using CqrsMovie.Muflone.Persistence;
using CqrsMovie.Seats.Domain.Entities;
using CqrsMovie.SharedKernel.Domain.Ids;
using Microsoft.Extensions.Logging;

namespace CqrsMovie.Seats.Domain.CommandHandlers
{
  public class CreateDailyProgrammingCommandHandler : CommandHandler<CreateDailyProgramming>
  {
    public CreateDailyProgrammingCommandHandler(IRepository repository, ILoggerFactory loggerFactory)
      : base(repository, loggerFactory)
    {
    }

    public override async Task Handle(CreateDailyProgramming command)
    {
      //TODO: Awful solution. Find a better one
      var entity = new DailyProgramming((DailyProgrammingId)command.AggregateId, command.MovieId, command.ScreenId, command.Date, command.Seats, command.MovieTitle, command.ScreenName);
      await Repository.Save(entity, Guid.NewGuid(), headers => { });
    }
  }
}