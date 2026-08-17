using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LebanonBasketballReservation.Data.Enums;

public enum StadiumStatus
{
    Pending = 0,
    Active = 1,
    Inactive = 2,
    Rejected = 3
}

public enum CourtStatus
{
    Active = 0,
    Inactive = 1,
    UnderMaintenance = 2
}

public enum ReservationStatus
{
    Pending = 0,
    Confirmed = 1,
    Cancelled = 2,
    Rejected = 3,
    Completed = 4
}

public enum NotificationType
{
    General = 0,
    ReservationConfirmed = 1,
    ReservationCancelled = 2,
    ReservationRejected = 3,
    ReservationReminder = 4,
    ReservationCompleted = 5,
    StadiumApproved = 6,
    StadiumRejected = 7
}
