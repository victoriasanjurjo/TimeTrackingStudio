using Microsoft.VisualStudio.TestTools.UnitTesting;
using TimeTrackingStudio.WinUI.Models;

namespace TimeTrackingStudio.Tests;

[TestClass]
public class ModelAndCalculationsTests
{
    [TestMethod]
    public void TimeLog_CalculatesHoursAndAmount_Accurately()
    {
        // 3600 segundos a 50.0/h
        var log = new TimeLog
        {
            Id = 1,
            JobId = 10,
            JobTitle = "Diseño UI",
            DurationSeconds = 7200, // 2 horas exactas
            HourlyRate = 65.0,
            IsBillable = true
        };

        Assert.AreEqual(2.0, log.Hours, 0.001);
        Assert.AreEqual(130.0, log.Amount, 0.001);
        Assert.AreEqual("02:00:00", log.FormattedDuration);
    }

    [TestMethod]
    public void TimeLog_NonBillable_ReturnsZeroAmount()
    {
        var log = new TimeLog
        {
            DurationSeconds = 3600,
            HourlyRate = 100.0,
            IsBillable = false
        };

        Assert.AreEqual(1.0, log.Hours, 0.001);
        Assert.AreEqual(0.0, log.Amount, 0.001);
    }

    [TestMethod]
    public void Job_CalculatesProgressPercentage_Accurately()
    {
        var job = new Job
        {
            Title = "Desarrollo Backend",
            EstimatedHours = 40.0,
            LoggedHours = 20.0
        };

        Assert.AreEqual(50.0, job.ProgressPercentage, 0.001);
    }

    [TestMethod]
    public void Client_DisplayRate_FormatsCorrectly()
    {
        var client = new Client
        {
            Name = "Acme Corp",
            HourlyRate = 75.5,
            Currency = "EUR"
        };

        Assert.AreEqual("75.50 EUR/h", client.DisplayRate);
    }
}
