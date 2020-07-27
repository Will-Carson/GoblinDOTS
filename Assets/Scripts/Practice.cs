using System;
using System.Collections.Generic;

namespace Practice
{
    public interface IParameter
    {
        bool IsApplicable(IParameter parameter);
    }

    public struct Memory : IParameter
    {
        public bool IsApplicable(IParameter parameter)
        {
            throw new NotImplementedException();
        }
    }

    public struct Values : IParameter
    {
        public bool IsApplicable(IParameter parameter)
        {
            throw new NotImplementedException();
        }
    }

    public struct Relationship : IParameter
    {
        public bool IsApplicable(IParameter parameter)
        {
            throw new NotImplementedException();
        }
    }

    public struct Play
    {
        
    }
}