using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using TestNinja.Mocking;

namespace TestNinja.UnitTests.Mocking
{
    [TestFixture]
    public class HouseKeeperServiceTests
    {
        private HouseKeeperService _service;
        private Mock<IStatementGenerator> _statementGenerator;
        private Mock<IEmailSender> _emailSender;
        private Mock<IXtraMessageBox> _messageBox;
        private DateTime _statementDate = new DateTime(2017, 1, 1);
        private Housekeeper _houseKeeper;
        private string _statementFileName;

        [SetUp]
        public void SetUp()
        {
            _houseKeeper = new Housekeeper { Email = "a@a.b", FullName = "b", Oid = 1, StatementEmailBody = "c" };

            var unitOfWork = new Mock<IUnitOfWork>();            
            unitOfWork.Setup(uow => uow.Query<Housekeeper>()).Returns(new List<Housekeeper>
            {
                _houseKeeper
            }.AsQueryable());

            _statementFileName = "fileName";
            _statementGenerator = new Mock<IStatementGenerator>();
            _statementGenerator
                .Setup(sg => sg.SaveStatement(_houseKeeper.Oid, _houseKeeper.FullName, _statementDate))
                .Returns(() => _statementFileName);

            _emailSender = new Mock<IEmailSender>();
            _messageBox = new Mock<IXtraMessageBox>();

            _service = new HouseKeeperService(
                unitOfWork.Object,
                _statementGenerator.Object,
                _emailSender.Object,
                _messageBox.Object
                );
        }

        [Test]
        [TestCase(null)]
        [TestCase(" ")]
        [TestCase("")]
        public void SendStatementEmails_WhenCalled_ShouldNotGenerateStatement(string email)
        {
            _houseKeeper.Email = email;

            _service.SendStatementEmails(_statementDate);

            _statementGenerator.Verify(sg =>
                sg.SaveStatement(_houseKeeper.Oid, _houseKeeper.FullName, _statementDate), Times.Never);
        }

        [Test]
        public void SendStatementEmails_WhenCalled_ShouldEmailTheStatement()
        {
            _service.SendStatementEmails(_statementDate);

            VerifyEmailSent();
        }

        [Test]
        [TestCase(null)]
        [TestCase(" ")]
        [TestCase("")]
        public void SendStatementEmails_WhenCalled_ShouldNotEmailTheStatement(string statementFileName)
        {
            _statementFileName = statementFileName;

            _service.SendStatementEmails(_statementDate);

            VerifyEmailNotSent();
        }

        private void VerifyEmailSent()
        {
            _emailSender.Verify(es => es.EmailFile(
                            _houseKeeper.Email,
                            _houseKeeper.StatementEmailBody,
                            _statementFileName,
                            It.IsAny<string>()));
        }

        private void VerifyEmailNotSent()
        {
            _emailSender.Verify(es => es.EmailFile(
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<string>(),
                            It.IsAny<string>()),
                            Times.Never);
        }
    }
}
