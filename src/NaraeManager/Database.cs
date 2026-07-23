using Microsoft.Data.Sqlite;

namespace NaraeManager;

public static class Database
{
    private static readonly string Folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NaraeManager");
    private static readonly string DbPath = Path.Combine(Folder, "narae-manager.db");
    private static string ConnectionString => $"Data Source={DbPath};Foreign Keys=True";

    public static void Initialize()
    {
        Directory.CreateDirectory(Folder);
        using var c = new SqliteConnection(ConnectionString);
        c.Open();
        var sql = @"
CREATE TABLE IF NOT EXISTS courses(
 id INTEGER PRIMARY KEY AUTOINCREMENT, year INTEGER NOT NULL, institution TEXT NOT NULL,
 name TEXT NOT NULL, course_code TEXT NOT NULL DEFAULT '', round_no INTEGER NOT NULL,
 start_date TEXT NOT NULL, end_date TEXT NOT NULL, weekdays TEXT NOT NULL,
 start_time TEXT NOT NULL, end_time TEXT NOT NULL, remote_type TEXT NOT NULL,
 time_type TEXT NOT NULL, category TEXT NOT NULL,
 UNIQUE(year,institution,name,round_no));
CREATE TABLE IF NOT EXISTS teacher_codes(
 id INTEGER PRIMARY KEY AUTOINCREMENT, course_id INTEGER NOT NULL,
 teacher_name TEXT NOT NULL, teacher_code TEXT NOT NULL, is_default INTEGER NOT NULL DEFAULT 0,
 FOREIGN KEY(course_id) REFERENCES courses(id) ON DELETE CASCADE);
CREATE TABLE IF NOT EXISTS room_codes(
 id INTEGER PRIMARY KEY AUTOINCREMENT, course_id INTEGER NOT NULL,
 room_name TEXT NOT NULL, room_code TEXT NOT NULL, is_default INTEGER NOT NULL DEFAULT 0,
 FOREIGN KEY(course_id) REFERENCES courses(id) ON DELETE CASCADE);
CREATE TABLE IF NOT EXISTS subject_codes(
 id INTEGER PRIMARY KEY AUTOINCREMENT, course_id INTEGER NOT NULL, sort_order INTEGER NOT NULL,
 subject_name TEXT NOT NULL, subject_code TEXT NOT NULL, allocated_hours INTEGER NOT NULL,
 teacher_code_id INTEGER NOT NULL, room_code_id INTEGER NOT NULL,
 FOREIGN KEY(course_id) REFERENCES courses(id) ON DELETE CASCADE,
 FOREIGN KEY(teacher_code_id) REFERENCES teacher_codes(id),
 FOREIGN KEY(room_code_id) REFERENCES room_codes(id));";
        using var cmd = c.CreateCommand(); cmd.CommandText = sql; cmd.ExecuteNonQuery();
    }

    public static List<Course> GetCourses()
    {
        var result = new List<Course>();
        using var c = Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = "SELECT * FROM courses ORDER BY year DESC,start_date,name,round_no";
        using var r = cmd.ExecuteReader();
        while (r.Read()) result.Add(ReadCourse(r));
        return result;
    }

    public static Course? GetCourse(long id)
    {
        using var c = Open(); using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT * FROM courses WHERE id=$id"; cmd.Parameters.AddWithValue("$id", id);
        using var r = cmd.ExecuteReader(); return r.Read() ? ReadCourse(r) : null;
    }

    public static long SaveCourse(Course x)
    {
        using var c = Open(); using var cmd = c.CreateCommand();
        if (x.Id == 0)
        {
            cmd.CommandText = @"INSERT INTO courses(year,institution,name,course_code,round_no,start_date,end_date,weekdays,start_time,end_time,remote_type,time_type,category)
VALUES($year,$institution,$name,$code,$round,$start,$end,$weekdays,$st,$et,$remote,$tt,$cat); SELECT last_insert_rowid();";
        }
        else
        {
            cmd.CommandText = @"UPDATE courses SET year=$year,institution=$institution,name=$name,course_code=$code,round_no=$round,start_date=$start,end_date=$end,weekdays=$weekdays,start_time=$st,end_time=$et,remote_type=$remote,time_type=$tt,category=$cat WHERE id=$id; SELECT $id;";
            cmd.Parameters.AddWithValue("$id", x.Id);
        }
        cmd.Parameters.AddWithValue("$year", x.Year); cmd.Parameters.AddWithValue("$institution", x.Institution); cmd.Parameters.AddWithValue("$name", x.Name);
        cmd.Parameters.AddWithValue("$code", x.CourseCode); cmd.Parameters.AddWithValue("$round", x.Round); cmd.Parameters.AddWithValue("$start", x.StartDate.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("$end", x.EndDate.ToString("yyyy-MM-dd")); cmd.Parameters.AddWithValue("$weekdays", x.Weekdays); cmd.Parameters.AddWithValue("$st", x.StartTime);
        cmd.Parameters.AddWithValue("$et", x.EndTime); cmd.Parameters.AddWithValue("$remote", x.RemoteType); cmd.Parameters.AddWithValue("$tt", x.TimeType); cmd.Parameters.AddWithValue("$cat", x.Category);
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    public static void DeleteCourse(long id) { using var c = Open(); using var cmd = c.CreateCommand(); cmd.CommandText = "DELETE FROM courses WHERE id=$id"; cmd.Parameters.AddWithValue("$id", id); cmd.ExecuteNonQuery(); }

    public static List<CourseTeacherCode> GetTeachers(long courseId) => QueryTeachers(courseId);
    public static List<CourseRoomCode> GetRooms(long courseId) => QueryRooms(courseId);
    public static List<CourseSubjectCode> GetSubjects(long courseId)
    {
        var list = new List<CourseSubjectCode>(); using var c = Open(); using var cmd = c.CreateCommand();
        cmd.CommandText = @"SELECT s.*,t.teacher_code,r.room_code FROM subject_codes s JOIN teacher_codes t ON t.id=s.teacher_code_id JOIN room_codes r ON r.id=s.room_code_id WHERE s.course_id=$id ORDER BY s.sort_order,s.id";
        cmd.Parameters.AddWithValue("$id", courseId); using var rd = cmd.ExecuteReader();
        while(rd.Read()) list.Add(new CourseSubjectCode{Id=rd.GetInt64(0),CourseId=rd.GetInt64(1),SortOrder=rd.GetInt32(2),SubjectName=rd.GetString(3),SubjectCode=rd.GetString(4),AllocatedHours=rd.GetInt32(5),TeacherCodeId=rd.GetInt64(6),RoomCodeId=rd.GetInt64(7),TeacherCode=rd.GetString(8),RoomCode=rd.GetString(9)});
        return list;
    }

    public static void AddTeacher(CourseTeacherCode x)
    {
        using var c=Open(); using var tx=c.BeginTransaction(); if(x.IsDefault){using var z=c.CreateCommand();z.Transaction=tx;z.CommandText="UPDATE teacher_codes SET is_default=0 WHERE course_id=$id";z.Parameters.AddWithValue("$id",x.CourseId);z.ExecuteNonQuery();}
        using var cmd=c.CreateCommand();cmd.Transaction=tx;cmd.CommandText="INSERT INTO teacher_codes(course_id,teacher_name,teacher_code,is_default) VALUES($c,$n,$x,$d)";cmd.Parameters.AddWithValue("$c",x.CourseId);cmd.Parameters.AddWithValue("$n",x.TeacherName);cmd.Parameters.AddWithValue("$x",x.TeacherCode);cmd.Parameters.AddWithValue("$d",x.IsDefault?1:0);cmd.ExecuteNonQuery();tx.Commit();
    }
    public static void AddRoom(CourseRoomCode x)
    {
        using var c=Open(); using var tx=c.BeginTransaction(); if(x.IsDefault){using var z=c.CreateCommand();z.Transaction=tx;z.CommandText="UPDATE room_codes SET is_default=0 WHERE course_id=$id";z.Parameters.AddWithValue("$id",x.CourseId);z.ExecuteNonQuery();}
        using var cmd=c.CreateCommand();cmd.Transaction=tx;cmd.CommandText="INSERT INTO room_codes(course_id,room_name,room_code,is_default) VALUES($c,$n,$x,$d)";cmd.Parameters.AddWithValue("$c",x.CourseId);cmd.Parameters.AddWithValue("$n",x.RoomName);cmd.Parameters.AddWithValue("$x",x.RoomCode);cmd.Parameters.AddWithValue("$d",x.IsDefault?1:0);cmd.ExecuteNonQuery();tx.Commit();
    }
    public static void AddSubject(CourseSubjectCode x){using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="INSERT INTO subject_codes(course_id,sort_order,subject_name,subject_code,allocated_hours,teacher_code_id,room_code_id) VALUES($c,$o,$n,$x,$h,$t,$r)";cmd.Parameters.AddWithValue("$c",x.CourseId);cmd.Parameters.AddWithValue("$o",x.SortOrder);cmd.Parameters.AddWithValue("$n",x.SubjectName);cmd.Parameters.AddWithValue("$x",x.SubjectCode);cmd.Parameters.AddWithValue("$h",x.AllocatedHours);cmd.Parameters.AddWithValue("$t",x.TeacherCodeId);cmd.Parameters.AddWithValue("$r",x.RoomCodeId);cmd.ExecuteNonQuery();}
    public static void DeleteCode(string table,long id){if(table is not("teacher_codes" or "room_codes" or "subject_codes"))throw new ArgumentException();using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText=$"DELETE FROM {table} WHERE id=$id";cmd.Parameters.AddWithValue("$id",id);cmd.ExecuteNonQuery();}

    private static SqliteConnection Open(){var c=new SqliteConnection(ConnectionString);c.Open();return c;}
    private static Course ReadCourse(SqliteDataReader r)=>new(){Id=r.GetInt64(0),Year=r.GetInt32(1),Institution=r.GetString(2),Name=r.GetString(3),CourseCode=r.GetString(4),Round=r.GetInt32(5),StartDate=DateTime.Parse(r.GetString(6)),EndDate=DateTime.Parse(r.GetString(7)),Weekdays=r.GetString(8),StartTime=r.GetString(9),EndTime=r.GetString(10),RemoteType=r.GetString(11),TimeType=r.GetString(12),Category=r.GetString(13)};
    private static List<CourseTeacherCode> QueryTeachers(long id){var list=new List<CourseTeacherCode>();using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT id,course_id,teacher_name,teacher_code,is_default FROM teacher_codes WHERE course_id=$id ORDER BY is_default DESC,id";cmd.Parameters.AddWithValue("$id",id);using var r=cmd.ExecuteReader();while(r.Read())list.Add(new(){Id=r.GetInt64(0),CourseId=r.GetInt64(1),TeacherName=r.GetString(2),TeacherCode=r.GetString(3),IsDefault=r.GetInt32(4)==1});return list;}
    private static List<CourseRoomCode> QueryRooms(long id){var list=new List<CourseRoomCode>();using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT id,course_id,room_name,room_code,is_default FROM room_codes WHERE course_id=$id ORDER BY is_default DESC,id";cmd.Parameters.AddWithValue("$id",id);using var r=cmd.ExecuteReader();while(r.Read())list.Add(new(){Id=r.GetInt64(0),CourseId=r.GetInt64(1),RoomName=r.GetString(2),RoomCode=r.GetString(3),IsDefault=r.GetInt32(4)==1});return list;}
}
