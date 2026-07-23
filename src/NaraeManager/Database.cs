using Microsoft.Data.Sqlite;

namespace NaraeManager;

public static class Database
{
    private static readonly string Folder = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NaraeManager");
    private static readonly string DbPath = System.IO.Path.Combine(Folder, "narae-manager.db");
    private static string ConnectionString => $"Data Source={DbPath};Foreign Keys=True";

    public static void Initialize()
    {
        System.IO.Directory.CreateDirectory(Folder);
        using var c = Open();
        Execute(c, "PRAGMA foreign_keys=ON");
        Execute(c, @"
CREATE TABLE IF NOT EXISTS institutions(
 id INTEGER PRIMARY KEY AUTOINCREMENT,
 name TEXT NOT NULL, business_number TEXT NOT NULL DEFAULT '', address TEXT NOT NULL DEFAULT '', phone TEXT NOT NULL DEFAULT '', email TEXT NOT NULL DEFAULT '', representative_name TEXT NOT NULL DEFAULT '', is_active INTEGER NOT NULL DEFAULT 1,
 UNIQUE(name,business_number));
CREATE TABLE IF NOT EXISTS operation_years(
 id INTEGER PRIMARY KEY AUTOINCREMENT, institution_id INTEGER NOT NULL, year INTEGER NOT NULL,
 UNIQUE(institution_id,year), FOREIGN KEY(institution_id) REFERENCES institutions(id));
CREATE TABLE IF NOT EXISTS training_courses(
 id INTEGER PRIMARY KEY AUTOINCREMENT, institution_id INTEGER NOT NULL, operation_year_id INTEGER NOT NULL,
 name TEXT NOT NULL, course_code TEXT NOT NULL, course_type TEXT NOT NULL, total_training_hours INTEGER NOT NULL, is_active INTEGER NOT NULL DEFAULT 1,
 UNIQUE(operation_year_id,course_code), FOREIGN KEY(institution_id) REFERENCES institutions(id), FOREIGN KEY(operation_year_id) REFERENCES operation_years(id));
CREATE TABLE IF NOT EXISTS course_rounds(
 id INTEGER PRIMARY KEY AUTOINCREMENT, course_id INTEGER NOT NULL, round_no INTEGER NOT NULL,
 start_date TEXT NOT NULL, end_date TEXT NOT NULL, weekdays TEXT NOT NULL, start_time TEXT NOT NULL, end_time TEXT NOT NULL, capacity INTEGER NOT NULL, status TEXT NOT NULL, total_training_hours INTEGER NOT NULL,
 UNIQUE(course_id,round_no), FOREIGN KEY(course_id) REFERENCES training_courses(id));
CREATE TABLE IF NOT EXISTS courses(
 id INTEGER PRIMARY KEY AUTOINCREMENT, year INTEGER NOT NULL, institution TEXT NOT NULL,
 name TEXT NOT NULL, course_code TEXT NOT NULL DEFAULT '', round_no INTEGER NOT NULL,
 start_date TEXT NOT NULL, end_date TEXT NOT NULL, weekdays TEXT NOT NULL,
 start_time TEXT NOT NULL, end_time TEXT NOT NULL, remote_type TEXT NOT NULL,
 time_type TEXT NOT NULL, category TEXT NOT NULL,
 UNIQUE(year,institution,name,round_no));
CREATE TABLE IF NOT EXISTS teacher_codes(
 id INTEGER PRIMARY KEY AUTOINCREMENT, course_id INTEGER NOT NULL, round_id INTEGER,
 teacher_name TEXT NOT NULL, teacher_code TEXT NOT NULL, is_default INTEGER NOT NULL DEFAULT 0,
 FOREIGN KEY(round_id) REFERENCES course_rounds(id));
CREATE TABLE IF NOT EXISTS room_codes(
 id INTEGER PRIMARY KEY AUTOINCREMENT, course_id INTEGER NOT NULL, round_id INTEGER,
 room_name TEXT NOT NULL, room_code TEXT NOT NULL, is_default INTEGER NOT NULL DEFAULT 0,
 FOREIGN KEY(round_id) REFERENCES course_rounds(id));
CREATE TABLE IF NOT EXISTS subject_codes(
 id INTEGER PRIMARY KEY AUTOINCREMENT, course_id INTEGER NOT NULL, round_id INTEGER, sort_order INTEGER NOT NULL,
 subject_name TEXT NOT NULL, subject_code TEXT NOT NULL, allocated_hours INTEGER NOT NULL,
 teacher_code_id INTEGER NOT NULL, room_code_id INTEGER NOT NULL,
 FOREIGN KEY(round_id) REFERENCES course_rounds(id),
 FOREIGN KEY(teacher_code_id) REFERENCES teacher_codes(id), FOREIGN KEY(room_code_id) REFERENCES room_codes(id));");
        AddColumn(c, "teacher_codes", "round_id", "INTEGER"); AddColumn(c, "room_codes", "round_id", "INTEGER"); AddColumn(c, "subject_codes", "round_id", "INTEGER");
    }

    public static List<Institution> GetInstitutions() => Query("SELECT id,name,business_number,address,phone,email,representative_name,is_active FROM institutions ORDER BY name", r => new Institution{Id=r.GetInt64(0),Name=r.GetString(1),BusinessNumber=r.GetString(2),Address=r.GetString(3),Phone=r.GetString(4),Email=r.GetString(5),RepresentativeName=r.GetString(6),IsActive=r.GetInt32(7)==1});
    public static long SaveInstitution(Institution x){Require(x.Name,"기관명"); using var c=Open(); using var cmd=c.CreateCommand(); if(x.Id==0) cmd.CommandText="INSERT INTO institutions(name,business_number,address,phone,email,representative_name,is_active) VALUES($n,$b,$a,$p,$e,$r,$u); SELECT last_insert_rowid();"; else {cmd.CommandText="UPDATE institutions SET name=$n,business_number=$b,address=$a,phone=$p,email=$e,representative_name=$r,is_active=$u WHERE id=$id; SELECT $id;"; cmd.Parameters.AddWithValue("$id",x.Id);} Add(cmd,"$n",x.Name);Add(cmd,"$b",x.BusinessNumber);Add(cmd,"$a",x.Address);Add(cmd,"$p",x.Phone);Add(cmd,"$e",x.Email);Add(cmd,"$r",x.RepresentativeName);Add(cmd,"$u",x.IsActive?1:0); return Convert.ToInt64(cmd.ExecuteScalar());}
    public static void DeleteInstitution(long id){EnsureUnused("operation_years","institution_id",id,"연도가 등록된 기관은 삭제할 수 없습니다."); Delete("institutions",id);}

    public static List<OperationYear> GetYears(long? inst=null)=>Query($"SELECT y.id,y.institution_id,i.name,y.year FROM operation_years y JOIN institutions i ON i.id=y.institution_id {(inst.HasValue?"WHERE y.institution_id=$id":"")} ORDER BY i.name,y.year DESC", r=>new OperationYear{Id=r.GetInt64(0),InstitutionId=r.GetInt64(1),InstitutionName=r.GetString(2),Year=r.GetInt32(3)}, inst);
    public static long SaveYear(OperationYear x){if(x.InstitutionId<=0)throw new InvalidOperationException("기관을 선택하세요."); if(x.Year<2000||x.Year>2100)throw new InvalidOperationException("연도는 2000~2100 사이여야 합니다."); using var c=Open(); using var cmd=c.CreateCommand(); cmd.CommandText="INSERT INTO operation_years(institution_id,year) VALUES($i,$y); SELECT last_insert_rowid();"; Add(cmd,"$i",x.InstitutionId);Add(cmd,"$y",x.Year); return Convert.ToInt64(cmd.ExecuteScalar());}
    public static void DeleteYear(long id){EnsureUnused("training_courses","operation_year_id",id,"과정이 등록된 연도는 삭제할 수 없습니다."); Delete("operation_years",id);}

    public static List<TrainingCourse> GetTrainingCourses()=>Query("SELECT c.id,c.institution_id,c.operation_year_id,i.name,y.year,c.name,c.course_code,c.course_type,c.total_training_hours,c.is_active FROM training_courses c JOIN institutions i ON i.id=c.institution_id JOIN operation_years y ON y.id=c.operation_year_id ORDER BY y.year DESC,i.name,c.name", r=>new TrainingCourse{Id=r.GetInt64(0),InstitutionId=r.GetInt64(1),OperationYearId=r.GetInt64(2),InstitutionName=r.GetString(3),Year=r.GetInt32(4),Name=r.GetString(5),CourseCode=r.GetString(6),CourseType=r.GetString(7),TotalTrainingHours=r.GetInt32(8),IsActive=r.GetInt32(9)==1});
    public static long SaveTrainingCourse(TrainingCourse x){Require(x.Name,"과정명");Require(x.CourseCode,"과정 ID"); if(x.TotalTrainingHours<=0)throw new InvalidOperationException("총 훈련시간은 1 이상이어야 합니다."); var y=GetYears().FirstOrDefault(v=>v.Id==x.OperationYearId)??throw new InvalidOperationException("연도를 선택하세요."); using var c=Open(); using var cmd=c.CreateCommand(); if(x.Id==0)cmd.CommandText="INSERT INTO training_courses(institution_id,operation_year_id,name,course_code,course_type,total_training_hours,is_active) VALUES($i,$y,$n,$code,$t,$h,$u); SELECT last_insert_rowid();"; else {cmd.CommandText="UPDATE training_courses SET institution_id=$i,operation_year_id=$y,name=$n,course_code=$code,course_type=$t,total_training_hours=$h,is_active=$u WHERE id=$id; SELECT $id;";Add(cmd,"$id",x.Id);} Add(cmd,"$i",y.InstitutionId);Add(cmd,"$y",x.OperationYearId);Add(cmd,"$n",x.Name);Add(cmd,"$code",x.CourseCode);Add(cmd,"$t",x.CourseType);Add(cmd,"$h",x.TotalTrainingHours);Add(cmd,"$u",x.IsActive?1:0); return Convert.ToInt64(cmd.ExecuteScalar());}
    public static void DeleteTrainingCourse(long id){EnsureUnused("course_rounds","course_id",id,"회차가 등록된 과정은 삭제할 수 없습니다."); Delete("training_courses",id);}

    public static List<CourseRound> GetRounds()=>Query("SELECT r.id,r.course_id,c.name,r.round_no,r.start_date,r.end_date,r.weekdays,r.start_time,r.end_time,r.capacity,r.status,r.total_training_hours FROM course_rounds r JOIN training_courses c ON c.id=r.course_id ORDER BY c.name,r.round_no", ReadRound);
    public static long SaveRound(CourseRound x){ValidateRound(x); using var c=Open(); using var cmd=c.CreateCommand(); if(x.Id==0)cmd.CommandText="INSERT INTO course_rounds(course_id,round_no,start_date,end_date,weekdays,start_time,end_time,capacity,status,total_training_hours) VALUES($c,$r,$sd,$ed,$w,$st,$et,$cap,$s,$h); SELECT last_insert_rowid();"; else {cmd.CommandText="UPDATE course_rounds SET course_id=$c,round_no=$r,start_date=$sd,end_date=$ed,weekdays=$w,start_time=$st,end_time=$et,capacity=$cap,status=$s,total_training_hours=$h WHERE id=$id; SELECT $id;";Add(cmd,"$id",x.Id);} AddRoundParams(cmd,x); return Convert.ToInt64(cmd.ExecuteScalar());}
    public static void DeleteRound(long id){EnsureUnused("teacher_codes","round_id",id,"코드가 등록된 회차는 삭제할 수 없습니다.");EnsureUnused("room_codes","round_id",id,"코드가 등록된 회차는 삭제할 수 없습니다.");EnsureUnused("subject_codes","round_id",id,"코드가 등록된 회차는 삭제할 수 없습니다.");Delete("course_rounds",id);}

    public static Course ToLegacyCourse(CourseRound r){var tc=GetTrainingCourses().First(x=>x.Id==r.CourseId); return new Course{Id=r.Id,Year=tc.Year,Institution=tc.InstitutionName,Name=tc.Name,CourseCode=tc.CourseCode,Round=r.RoundNo,StartDate=r.StartDate,EndDate=r.EndDate,Weekdays=r.Weekdays,StartTime=r.StartTime,EndTime=r.EndTime,Category=tc.CourseType};}

    public static List<Course> GetCourses()=>GetRounds().Select(ToLegacyCourse).ToList();
    public static List<CourseTeacherCode> GetTeachers(long roundId)=>Query("SELECT id,course_id,COALESCE(round_id,0),teacher_name,teacher_code,is_default FROM teacher_codes WHERE round_id=$id OR (round_id IS NULL AND course_id=$id) ORDER BY is_default DESC,id", r=>new CourseTeacherCode{Id=r.GetInt64(0),CourseId=r.GetInt64(1),RoundId=r.GetInt64(2),TeacherName=r.GetString(3),TeacherCode=r.GetString(4),IsDefault=r.GetInt32(5)==1}, roundId);
    public static List<CourseRoomCode> GetRooms(long roundId)=>Query("SELECT id,course_id,COALESCE(round_id,0),room_name,room_code,is_default FROM room_codes WHERE round_id=$id OR (round_id IS NULL AND course_id=$id) ORDER BY is_default DESC,id", r=>new CourseRoomCode{Id=r.GetInt64(0),CourseId=r.GetInt64(1),RoundId=r.GetInt64(2),RoomName=r.GetString(3),RoomCode=r.GetString(4),IsDefault=r.GetInt32(5)==1}, roundId);
    public static List<CourseSubjectCode> GetSubjects(long roundId)=>Query("SELECT s.id,s.course_id,COALESCE(s.round_id,0),s.sort_order,s.subject_name,s.subject_code,s.allocated_hours,s.teacher_code_id,s.room_code_id,t.teacher_code,r.room_code FROM subject_codes s JOIN teacher_codes t ON t.id=s.teacher_code_id JOIN room_codes r ON r.id=s.room_code_id WHERE s.round_id=$id OR (s.round_id IS NULL AND s.course_id=$id) ORDER BY s.sort_order,s.id", r=>new CourseSubjectCode{Id=r.GetInt64(0),CourseId=r.GetInt64(1),RoundId=r.GetInt64(2),SortOrder=r.GetInt32(3),SubjectName=r.GetString(4),SubjectCode=r.GetString(5),AllocatedHours=r.GetInt32(6),TeacherCodeId=r.GetInt64(7),RoomCodeId=r.GetInt64(8),TeacherCode=r.GetString(9),RoomCode=r.GetString(10)}, roundId);
    public static void AddTeacher(CourseTeacherCode x){Require(x.TeacherName,"강사명");Require(x.TeacherCode,"강사코드"); using var c=Open(); using var tx=c.BeginTransaction(); if(x.IsDefault)Execute(c,"UPDATE teacher_codes SET is_default=0 WHERE round_id=$id",tx,x.RoundId); using var cmd=c.CreateCommand();cmd.Transaction=tx;cmd.CommandText="INSERT INTO teacher_codes(course_id,round_id,teacher_name,teacher_code,is_default) VALUES($c,$r,$n,$x,$d)";Add(cmd,"$c",x.CourseId);Add(cmd,"$r",x.RoundId);Add(cmd,"$n",x.TeacherName);Add(cmd,"$x",x.TeacherCode);Add(cmd,"$d",x.IsDefault?1:0);cmd.ExecuteNonQuery();tx.Commit();}
    public static void AddRoom(CourseRoomCode x){Require(x.RoomName,"강의실명");Require(x.RoomCode,"강의실코드"); using var c=Open(); using var tx=c.BeginTransaction(); if(x.IsDefault)Execute(c,"UPDATE room_codes SET is_default=0 WHERE round_id=$id",tx,x.RoundId); using var cmd=c.CreateCommand();cmd.Transaction=tx;cmd.CommandText="INSERT INTO room_codes(course_id,round_id,room_name,room_code,is_default) VALUES($c,$r,$n,$x,$d)";Add(cmd,"$c",x.CourseId);Add(cmd,"$r",x.RoundId);Add(cmd,"$n",x.RoomName);Add(cmd,"$x",x.RoomCode);Add(cmd,"$d",x.IsDefault?1:0);cmd.ExecuteNonQuery();tx.Commit();}
    public static void AddSubject(CourseSubjectCode x){Require(x.SubjectName,"교과목명");Require(x.SubjectCode,"교과목코드"); if(x.AllocatedHours<=0)throw new InvalidOperationException("배정시간은 1 이상이어야 합니다."); using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText="INSERT INTO subject_codes(course_id,round_id,sort_order,subject_name,subject_code,allocated_hours,teacher_code_id,room_code_id) VALUES($c,$r,$o,$n,$x,$h,$t,$m)";Add(cmd,"$c",x.CourseId);Add(cmd,"$r",x.RoundId);Add(cmd,"$o",x.SortOrder);Add(cmd,"$n",x.SubjectName);Add(cmd,"$x",x.SubjectCode);Add(cmd,"$h",x.AllocatedHours);Add(cmd,"$t",x.TeacherCodeId);Add(cmd,"$m",x.RoomCodeId);cmd.ExecuteNonQuery();}
    public static void DeleteCode(string table,long id){if(table is not("teacher_codes" or "room_codes" or "subject_codes"))throw new ArgumentException();Delete(table,id);}

    private static SqliteConnection Open(){var c=new SqliteConnection(ConnectionString);c.Open();using var cmd=c.CreateCommand();cmd.CommandText="PRAGMA foreign_keys=ON";cmd.ExecuteNonQuery();return c;}
    private static void Add(SqliteCommand c,string n,object? v)=>c.Parameters.AddWithValue(n,v??DBNull.Value);
    private static void Execute(SqliteConnection c,string sql,SqliteTransaction? tx=null,long id=0){using var cmd=c.CreateCommand();cmd.Transaction=tx;cmd.CommandText=sql;if(sql.Contains("$id"))Add(cmd,"$id",id);cmd.ExecuteNonQuery();}
    private static void AddColumn(SqliteConnection c,string table,string name,string def){try{Execute(c,$"ALTER TABLE {table} ADD COLUMN {name} {def}");}catch(SqliteException ex) when(ex.SqliteErrorCode==1 && ex.Message.Contains("duplicate column")){}}
    private static List<T> Query<T>(string sql,Func<SqliteDataReader,T> map,long? id=null){var list=new List<T>();using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText=sql;if(id.HasValue)Add(cmd,"$id",id.Value);using var r=cmd.ExecuteReader();while(r.Read())list.Add(map(r));return list;}
    private static void Delete(string table,long id){using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText=$"DELETE FROM {table} WHERE id=$id";Add(cmd,"$id",id);cmd.ExecuteNonQuery();}
    private static void EnsureUnused(string table,string col,long id,string msg){using var c=Open();using var cmd=c.CreateCommand();cmd.CommandText=$"SELECT COUNT(*) FROM {table} WHERE {col}=$id";Add(cmd,"$id",id);if(Convert.ToInt32(cmd.ExecuteScalar())>0)throw new InvalidOperationException(msg);}
    private static void Require(string v,string label){if(string.IsNullOrWhiteSpace(v))throw new InvalidOperationException($"{label}은(는) 필수입니다.");}
    private static CourseRound ReadRound(SqliteDataReader r)=>new(){Id=r.GetInt64(0),CourseId=r.GetInt64(1),CourseName=r.GetString(2),RoundNo=r.GetInt32(3),StartDate=DateTime.Parse(r.GetString(4)),EndDate=DateTime.Parse(r.GetString(5)),Weekdays=r.GetString(6),StartTime=r.GetString(7),EndTime=r.GetString(8),Capacity=r.GetInt32(9),Status=r.GetString(10),TotalTrainingHours=r.GetInt32(11)};
    private static void ValidateRound(CourseRound x){if(x.CourseId<=0)throw new InvalidOperationException("과정을 선택하세요.");if(x.StartDate>x.EndDate)throw new InvalidOperationException("종료일은 시작일보다 빠를 수 없습니다.");if(!int.TryParse(x.StartTime,out var st)||!int.TryParse(x.EndTime,out var et)||st>=et)throw new InvalidOperationException("종료시간은 시작시간보다 늦어야 합니다.");if(x.Capacity<=0)throw new InvalidOperationException("정원은 1 이상이어야 합니다.");if(x.TotalTrainingHours<=0)throw new InvalidOperationException("총 훈련시간은 1 이상이어야 합니다.");Require(x.Weekdays,"수업 요일");Require(x.Status,"상태");}
    private static void AddRoundParams(SqliteCommand cmd,CourseRound x){Add(cmd,"$c",x.CourseId);Add(cmd,"$r",x.RoundNo);Add(cmd,"$sd",x.StartDate.ToString("yyyy-MM-dd"));Add(cmd,"$ed",x.EndDate.ToString("yyyy-MM-dd"));Add(cmd,"$w",x.Weekdays);Add(cmd,"$st",x.StartTime);Add(cmd,"$et",x.EndTime);Add(cmd,"$cap",x.Capacity);Add(cmd,"$s",x.Status);Add(cmd,"$h",x.TotalTrainingHours);}
}
