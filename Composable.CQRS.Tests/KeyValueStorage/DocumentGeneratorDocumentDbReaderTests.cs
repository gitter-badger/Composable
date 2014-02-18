﻿using System;
using System.Collections.Generic;
using System.Linq;
using Composable.DDD;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.AutoGenerated;
using Composable.System;
using Composable.System.Linq;
using Composable.SystemExtensions.Threading;
using FluentAssertions;
using Moq;

namespace CQRS.Tests.KeyValueStorage
{
    public class DocumentGeneratorDocumentDbReaderTests : NSpec.NUnit.nspec
    {
        public void given_two_cats_with_ids_1_and_2_and_two_dogs_with_ids_3_and_4()
        {
            IDocumentDbReader reader = null;
            Mock<IDocumentDbSessionInterceptor> interceptorMock = null;
            var cat1Id = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var cat2Id = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var dog1Id = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var dog2Id = Guid.Parse("00000000-0000-0000-0000-000000000004");

            before = () =>
                     {
                         interceptorMock = new Mock<IDocumentDbSessionInterceptor>(MockBehavior.Strict);
                         interceptorMock.Setup(interceptor => interceptor.AfterLoad(It.IsAny<object>())).Verifiable();

                         reader = new DocumentGeneratorDocumentDbReader(new SingleThreadUseGuard(),
                             interceptorMock.Object,
                             new List<IDocumentGenerator> {new DogGenerator(), new CatGenerator()});
                     };
            context["when you Get a cat with id {0}".FormatWith(cat1Id)] =
                () =>
                {
                    Cat aCat = null;
                    act = () => aCat = reader.Get<Cat>(cat1Id);
                    it["the fetched cat has the id: {0}".FormatWith(cat1Id)] = () => aCat.Id.Should().Be(cat1Id);
                    it["the interceptor is called once"] = () => interceptorMock.Verify(interceptor => interceptor.AfterLoad(aCat), Times.Exactly(1));
                    context["when you Get a cat with id {0} again".FormatWith(cat1Id)] =
                        () =>
                        {
                            Cat theSameCat = null;
                            act = () => theSameCat = reader.Get<Cat>(cat1Id);
                            it["the cat has the id: {0}".FormatWith(cat1Id)] = () => theSameCat.Id.Should().Be(cat1Id);
                            it["both fetched cats are the same instance"] = () => aCat.Should().Be(theSameCat);
                            it["the interceptor is not called again"] = () => interceptorMock.Verify(interceptor => interceptor.AfterLoad(theSameCat), Times.Exactly(1));
                        };
                };

            context["when you Get a Dog with id {0}".FormatWith(dog1Id)] =
                () =>
                {
                    Dog aDog = null;
                    act = () => aDog = reader.Get<Dog>(dog1Id);
                    it["the fetched dog has the id: {0}".FormatWith(dog1Id)] = () => aDog.Id.Should().Be(dog1Id);
                    it["the interceptor is called once"] = () => interceptorMock.Verify(interceptor => interceptor.AfterLoad(aDog), Times.Exactly(1));
                    context["when you Get a dog with id {0} again".FormatWith(dog1Id)] =
                        () =>
                        {
                            Dog theSameDog = null;
                            act = () => theSameDog = reader.Get<Dog>(dog1Id);
                            it["the fetched dog has the id: {0}".FormatWith(dog1Id)] = () => theSameDog.Id.Should().Be(dog1Id);
                            it["both fetched dogs are the same instance"] = () => aDog.Should().Be(theSameDog);
                            it["the interceptor is not called again"] = () => interceptorMock.Verify(interceptor => interceptor.AfterLoad(theSameDog), Times.Exactly(1));
                        };
                };

            context["when you Get an animal with id {0}".FormatWith(dog1Id)] =
                () =>
                {
                    Animal aDog = null;
                    act = () => aDog = reader.Get<Animal>(dog1Id);
                    it["the fetched animal is a Dog"] = () => aDog.Should().BeOfType<Dog>();
                    it["the Dog has the id: {0}".FormatWith(dog1Id)] = () => aDog.Id.Should().Be(dog1Id);
                    it["the interceptor is called once"] = () => interceptorMock.Verify(interceptor => interceptor.AfterLoad(aDog), Times.Exactly(1));
                    context["when you Get an animal with id {0} again".FormatWith(dog1Id)] =
                        () =>
                        {
                            Animal theSameDog = null;
                            act = () => theSameDog = reader.Get<Animal>(dog1Id);
                            it["the fetched animal is a Dog"] = () => theSameDog.Should().BeOfType<Dog>();
                            it["the dog has the id: {0}".FormatWith(dog1Id)] = () => theSameDog.Id.Should().Be(dog1Id);
                            it["both fetched dogs are the same instance"] = () => aDog.Should().Be(theSameDog);
                            it["the interceptor is not called again"] = () => interceptorMock.Verify(interceptor => interceptor.AfterLoad(theSameDog), Times.Exactly(1));
                        };
                };
            context["when you Get animals with ids 1,2,3,4"] =
                () =>
                {
                    IEnumerable<Animal> animals = null;
                    act = () => animals = reader.Get<Animal>(Seq.Create(cat1Id, cat2Id, dog1Id, dog2Id));
                    it["there are 4 animals returned"] = () => animals.Should().HaveCount(4);
                    it["there is a cat with id 1"] = () => animals.OfType<Cat>().Where(cat => cat.Id == cat1Id).Should().HaveCount(1);
                    it["there is a cat with id 2"] = () => animals.OfType<Cat>().Where(cat => cat.Id == cat2Id).Should().HaveCount(1);
                    it["there is a dog with id 3"] = () => animals.OfType<Dog>().Where(cat => cat.Id == dog1Id).Should().HaveCount(1);
                    it["there is a dog with id 4"] = () => animals.OfType<Dog>().Where(cat => cat.Id == dog2Id).Should().HaveCount(1);
                    it["the interceptor is called 4 times"] = () => interceptorMock.Verify(interceptor => interceptor.AfterLoad(It.IsAny<Animal>()), Times.Exactly(4));
                    it["the interceptor is called once for the first animal"] = () => interceptorMock.Verify(interceptor => interceptor.AfterLoad(animals.ElementAt(0)), Times.Exactly(1));
                    it["the interceptor is called once for the second animal"] = () => interceptorMock.Verify(interceptor => interceptor.AfterLoad(animals.ElementAt(1)), Times.Exactly(1));
                    it["the interceptor is called once for the third animal"] = () => interceptorMock.Verify(interceptor => interceptor.AfterLoad(animals.ElementAt(2)), Times.Exactly(1));
                    it["the interceptor is called once for the fourth animal"] = () => interceptorMock.Verify(interceptor => interceptor.AfterLoad(animals.ElementAt(3)), Times.Exactly(1));
                    context["when you Get animals with ids 1,2,3,4 again"] =
                        () =>
                        {
                            IEnumerable<Animal> theSameAnimals = null;
                            act = () => theSameAnimals = reader.Get<Animal>(Seq.Create(cat1Id, cat2Id, dog1Id, dog2Id));
                            it["there are 4 animals returned"] = () => theSameAnimals.Should().HaveCount(4);
                            it["the returned animals are the same instances as from the last call"] = () => animals.Should().Equal(theSameAnimals);
                            it["the interceptor has not been called again"] = () => interceptorMock.Verify(interceptor => interceptor.AfterLoad(It.IsAny<Animal>()), Times.Exactly(4));
                        };
                };
        }

        public class Animal : PersistentEntity<Animal>
        {
            protected Animal(Guid id) : base(id) {}
        }

        public class Dog : Animal
        {
            public Dog(Guid id) : base(id) {}
        }

        public class Cat : Animal
        {
            public Cat(Guid id) : base(id) {}
        }

        public class DogGenerator : IDocumentGenerator<Dog>
        {
            public Dog TryGenerate(object id)
            {
                var intId = id.ToString().Last().Transform(value => int.Parse(new string(value, 1)));
                if(intId > 4 || intId < 3)
                {
                    return null;
                }
                return new Dog((Guid)id);
            }
        }

        public class CatGenerator : IDocumentGenerator<Cat>
        {
            public Cat TryGenerate(object id)
            {
                var intId = id.ToString().Last().Transform(value => int.Parse(new string(value, 1)));
                if(intId > 2)
                {
                    return null;
                }
                return new Cat((Guid)id);
            }
        }
    }
}
