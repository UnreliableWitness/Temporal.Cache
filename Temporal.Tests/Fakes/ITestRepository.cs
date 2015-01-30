﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Temporal.Tests.Mocks;

namespace Temporal.Tests.Fakes
{
    public interface ITestRepository
    {
        Task<IEnumerable<Person>> RetrievePersonsAsync();
        IEnumerable<Person> RetrievePersons();
        Person RetrievePerson(int id);
        Person RetrievePerson(Person person);
        Person RetrievePerson(Person person, int i);
    }
}