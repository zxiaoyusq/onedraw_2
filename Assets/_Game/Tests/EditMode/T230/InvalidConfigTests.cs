using System;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OneStrokeDemon.Config;

namespace OneStrokeDemon.Tests.EditMode.T230
{
    [Category("ConfigPipeline")]
    public sealed class InvalidConfigTests
    {
        [SetUp]
        public void SetUp()
        {
            GameplayConfigRuntime.ResetForTests();
        }

        [Test]
        public void EmptyJsonFailsAndServiceCannotRetryOrPublishPartialState()
        {
            var service = new GameplayConfigService();

            AssertFailure(service, "CFGRT001", string.Empty);
            Assert.That(service.State, Is.EqualTo(GameplayConfigServiceState.Failed));
            Assert.That(service.Summary, Is.Null);
            Assert.That(Assert.Throws<GameplayConfigException>(() => service.GetEnemy("boss_tomb_king")).Code, Is.EqualTo("CFGRT001"));

            GameplayConfigException retry = Assert.Throws<GameplayConfigException>(() =>
                service.Load(RuntimeConfigTestFixture.LoadJson(), "test:retry"));
            Assert.That(retry.Code, Is.EqualTo("CFGRT001"));
            Assert.That(service.State, Is.EqualTo(GameplayConfigServiceState.Failed));
        }

        [Test]
        public void UnsupportedSchemaVersionFailsBeforePublication()
        {
            string json = RuntimeConfigTestFixture.MutateAndRehash(root => root["schemaVersion"] = 999);

            AssertFailure(new GameplayConfigService(), "CFGRT003", json);
            Assert.Throws<GameplayConfigException>(() => GameplayConfigRuntime.Initialize(json, "test:runtime-schema"));
            Assert.That(GameplayConfigRuntime.IsReady, Is.False);
        }

        [Test]
        public void IncompatibleAndMalformedContentVersionsFail()
        {
            string incompatible = RuntimeConfigTestFixture.MutateAndRehash(root => root["contentVersion"] = "1.0.0");
            string malformed = RuntimeConfigTestFixture.MutateAndRehash(root => root["contentVersion"] = "latest");

            AssertFailure(new GameplayConfigService(), "CFGRT004", incompatible);
            AssertFailure(new GameplayConfigService(), "CFGRT004", malformed);
        }

        [Test]
        public void MissingNullAndUnknownMembersFailStrictJsonContract()
        {
            string missing = RuntimeConfigTestFixture.MutateAndRehash(root => root.Remove("enemies"));
            string nullTable = RuntimeConfigTestFixture.MutateAndRehash(root => root["levels"] = JValue.CreateNull());
            string unknown = RuntimeConfigTestFixture.MutateAndRehash(root => root["unexpectedRootValue"] = 1);

            AssertFailure(new GameplayConfigService(), "CFGRT002", missing);
            AssertFailure(new GameplayConfigService(), "CFGRT002", nullTable);
            AssertFailure(new GameplayConfigService(), "CFGRT002", unknown);
        }

        [Test]
        public void DuplicateJsonPropertyFailsStrictParsing()
        {
            string source = RuntimeConfigTestFixture.LoadJson();
            const string firstProperty = "\"schemaVersion\": 3,";
            string duplicate = source.Replace(
                firstProperty,
                firstProperty + "\n  \"schemaVersion\": 3,");

            AssertFailure(new GameplayConfigService(), "CFGRT002", duplicate);
        }

        [Test]
        public void JsonCommentsFailStrictParsing()
        {
            string commented = "// generated files must remain standard JSON\n" +
                RuntimeConfigTestFixture.LoadJson();

            AssertFailure(new GameplayConfigService(), "CFGRT002", commented);
        }

        [Test]
        public void EmptyOrDuplicateRuntimeIndexKeyRejectsWholeDocument()
        {
            string empty = RuntimeConfigTestFixture.MutateAndRehash(root =>
                root["enemies"][0]["enemyId"] = string.Empty);
            string duplicate = RuntimeConfigTestFixture.MutateAndRehash(root =>
                root["enemies"][1]["enemyId"] = root["enemies"][0]["enemyId"].Value<string>());

            AssertFailure(new GameplayConfigService(), "CFGRT006", empty);
            AssertFailure(new GameplayConfigService(), "CFGRT006", duplicate);
        }

        [Test]
        public void TamperedContentAndMalformedHashFailHashVerification()
        {
            JObject tamperedRoot = RuntimeConfigTestFixture.LoadRoot();
            tamperedRoot["enemies"][0]["maxHp"] = 999999;
            string tampered = tamperedRoot.ToString(Newtonsoft.Json.Formatting.None);
            string malformed = RuntimeConfigTestFixture.MutateAndRehash(root => { });
            JObject malformedRoot = JObject.Parse(malformed);
            malformedRoot["contentHash"] = "ABC";

            AssertFailure(new GameplayConfigService(), "CFGRT005", tampered);
            AssertFailure(new GameplayConfigService(), "CFGRT005", malformedRoot.ToString());
        }

        [Test]
        public void RootAndGlobalVersionsMustAgreeEvenWhenHashIsValid()
        {
            string json = RuntimeConfigTestFixture.MutateAndRehash(root => root["contentVersion"] = "0.3.1");

            GameplayConfigException exception = AssertFailure(new GameplayConfigService(), "CFGRT004", json);
            Assert.That(exception.Context, Is.EqualTo("Global.content_version"));
        }

        private static GameplayConfigException AssertFailure(
            GameplayConfigService service,
            string expectedCode,
            string json)
        {
            GameplayConfigException exception = Assert.Throws<GameplayConfigException>(() =>
                service.Load(json, RuntimeConfigTestFixture.Source));
            Assert.That(exception.Code, Is.EqualTo(expectedCode));
            Assert.That(exception.Source, Is.EqualTo(RuntimeConfigTestFixture.Source));
            Assert.That(service.State, Is.EqualTo(GameplayConfigServiceState.Failed));
            return exception;
        }
    }
}
