namespace NaraeManager;

public sealed class Institution
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public string BusinessNumber { get; set; } = "";
    public string Address { get; set; } = "";
    public string Phone { get; set; } = "";
    public string Email { get; set; } = "";
    public string RepresentativeName { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public string Display => $"{Name} ({BusinessNumber})";
}

public sealed class OperationYear
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public string InstitutionName { get; set; } = "";
    public int Year { get; set; } = DateTime.Today.Year;
    public string Display => $"{InstitutionName} - {Year}";
}

public sealed class TrainingCourse
{
    public long Id { get; set; }
    public long InstitutionId { get; set; }
    public long OperationYearId { get; set; }
    public string InstitutionName { get; set; } = "";
    public int Year { get; set; } = DateTime.Today.Year;
    public string Name { get; set; } = "";
    public string CourseCode { get; set; } = "";
    public string CourseType { get; set; } = "SNS";
    public int TotalTrainingHours { get; set; }
    public bool IsActive { get; set; } = true;
    public string Display => $"{Year} | {InstitutionName} | {Name}";
}

public sealed class CourseRound
{
    public long Id { get; set; }
    public long CourseId { get; set; }
    public string CourseName { get; set; } = "";
    public int RoundNo { get; set; } = 1;
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today;
    public string Weekdays { get; set; } = "0,1,2,3,4";
    public string StartTime { get; set; } = "1400";
    public string EndTime { get; set; } = "1800";
    public int Capacity { get; set; } = 20;
    public string Status { get; set; } = "운영예정";
    public int TotalTrainingHours { get; set; }
    public string Display => $"{CourseName} | {RoundNo}회차";
}

public sealed class Course
{
    public long Id { get; set; }
    public int Year { get; set; } = DateTime.Today.Year;
    public string Institution { get; set; } = "나래직업훈련아카데미";
    public string Name { get; set; } = "";
    public string CourseCode { get; set; } = "";
    public int Round { get; set; } = 1;
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime EndDate { get; set; } = DateTime.Today;
    public string Weekdays { get; set; } = "0,1,2,3,4";
    public string StartTime { get; set; } = "1400";
    public string EndTime { get; set; } = "1800";
    public string RemoteType { get; set; } = "14";
    public string TimeType { get; set; } = "1";
    public string Category { get; set; } = "SNS";
    public string Display => $"{Year} | {Name} | {Round}회";
}

public sealed class CourseTeacherCode
{
    public long Id { get; set; }
    public long CourseId { get; set; }
    public long RoundId { get; set; }
    public string TeacherName { get; set; } = "";
    public string TeacherCode { get; set; } = "";
    public bool IsDefault { get; set; }
}

public sealed class CourseRoomCode
{
    public long Id { get; set; }
    public long CourseId { get; set; }
    public long RoundId { get; set; }
    public string RoomName { get; set; } = "";
    public string RoomCode { get; set; } = "";
    public bool IsDefault { get; set; }
}

public sealed class CourseSubjectCode
{
    public long Id { get; set; }
    public long CourseId { get; set; }
    public long RoundId { get; set; }
    public int SortOrder { get; set; }
    public string SubjectName { get; set; } = "";
    public string SubjectCode { get; set; } = "";
    public int AllocatedHours { get; set; }
    public long TeacherCodeId { get; set; }
    public long RoomCodeId { get; set; }
    public string TeacherCode { get; set; } = "";
    public string RoomCode { get; set; } = "";
}

public sealed record ScheduleRow(string TrainingDate,string TrainingStart,string TrainingEnd,string RemoteType,string SlotStart,string TimeType,string TeacherCode,string RoomCode,string SubjectCode);
