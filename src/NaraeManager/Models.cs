namespace NaraeManager;

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
    public string TeacherName { get; set; } = "";
    public string TeacherCode { get; set; } = "";
    public bool IsDefault { get; set; }
}

public sealed class CourseRoomCode
{
    public long Id { get; set; }
    public long CourseId { get; set; }
    public string RoomName { get; set; } = "";
    public string RoomCode { get; set; } = "";
    public bool IsDefault { get; set; }
}

public sealed class CourseSubjectCode
{
    public long Id { get; set; }
    public long CourseId { get; set; }
    public int SortOrder { get; set; }
    public string SubjectName { get; set; } = "";
    public string SubjectCode { get; set; } = "";
    public int AllocatedHours { get; set; }
    public long TeacherCodeId { get; set; }
    public long RoomCodeId { get; set; }
    public string TeacherCode { get; set; } = "";
    public string RoomCode { get; set; } = "";
}

public sealed record ScheduleRow(
    string TrainingDate,
    string TrainingStart,
    string TrainingEnd,
    string RemoteType,
    string SlotStart,
    string TimeType,
    string TeacherCode,
    string RoomCode,
    string SubjectCode);
