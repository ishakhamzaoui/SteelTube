using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SteelTube.Domain.ValueObjects;

namespace SteelTube.Domain.Tests
{
    [TestClass]
    public class InventoryOperationTests
    {
        private readonly Guid _tubeSpecificationId = Guid.NewGuid();
        private readonly Guid _deviceId = Guid.NewGuid();
        private readonly DateTime _now = DateTime.UtcNow;

        [TestMethod]
        public void Purchase_has_positive_signed_length()
        {
            var op = SteelTube.Domain.Entities.InventoryOperation.CreatePurchase(
                _tubeSpecificationId, Length.FromMeters(50m), null, null, null, null,
                _now, _now, _deviceId, 1, null);

            op.SignedLengthMeters.Should().Be(50m);
        }

        [TestMethod]
        public void Sale_has_negative_signed_length()
        {
            var op = SteelTube.Domain.Entities.InventoryOperation.CreateSale(
                _tubeSpecificationId, Length.FromMeters(20m), null, null, null, null,
                _now, _now, _deviceId, 2, null);

            op.SignedLengthMeters.Should().Be(-20m);
        }

        [TestMethod]
        public void AdjustmentIncrease_requires_a_note()
        {
            Action act = () => SteelTube.Domain.Entities.InventoryOperation.CreateAdjustmentIncrease(
                _tubeSpecificationId, Length.FromMeters(5m), null, null, _now, _now, _deviceId, 3, note: null);

            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void Two_operations_never_share_an_OperationId()
        {
            var op1 = SteelTube.Domain.Entities.InventoryOperation.CreatePurchase(
                _tubeSpecificationId, Length.FromMeters(10m), null, null, null, null, _now, _now, _deviceId, 1, null);
            var op2 = SteelTube.Domain.Entities.InventoryOperation.CreatePurchase(
                _tubeSpecificationId, Length.FromMeters(10m), null, null, null, null, _now, _now, _deviceId, 2, null);

            op1.OperationId.Should().NotBe(op2.OperationId);
        }
    }
}