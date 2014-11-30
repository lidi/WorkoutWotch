﻿using System;
using Kent.Boogaart.HelperTrinity.Extensions;

namespace WorkoutWotch.Models.Events
{
    public abstract class EventBase : IEvent
    {
        private readonly ExecutionContext executionContext;

        protected EventBase(ExecutionContext executionContext)
        {
            executionContext.AssertNotNull("executionContext");
            this.executionContext = executionContext;
        }

        public ExecutionContext ExecutionContext
        {
            get{return this.executionContext;}
        }
    }
}

