﻿using System;
using System.Collections.Generic;

namespace Quidjibo
{
    public class QuidjiboBuilderException : Exception
    {
        public List<string> Errors;

        public QuidjiboBuilderException(List<string> errors, string message) : base(message)
        {
            Errors = errors;
        }
    }
}