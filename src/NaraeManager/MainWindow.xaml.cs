using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace NaraeManager;

public partial class MainWindow : Window
{
    private long _selectedCourseId;
    private List<Course> _courses = [];

    public MainWindow()
    {
        InitializeComponent();
        ReloadCourses();
        StartDateBox.Text = DateTime.Today.ToString("yyyy-MM-dd");
        EndDateBox.Text = DateTime.Today.ToString("yyyy-MM-dd");
    }

    private void ReloadCourses()
    {
        _courses = Database.GetCourses();
        CourseGrid.ItemsSource = null; CourseGrid.ItemsSource = _courses;
        CodeCourseBox.ItemsSource = _courses; ScheduleCourseBox.ItemsSource = _courses; DiaryCourseBox.ItemsSource = _courses;
    }

    private Course ReadCourseForm() => new()
    {
        Id = _selectedCourseId,
        Year = int.Parse(YearBox.Text), Institution = InstitutionBox.Text.Trim(), Name = CourseNameBox.Text.Trim(), CourseCode = CourseCodeBox.Text.Trim(),
        Round = int.Parse(RoundBox.Text), StartDate = DateTime.Parse(StartDateBox.Text), EndDate = DateTime.Parse(EndDateBox.Text), Weekdays = WeekdaysBox.Text.Trim(),
        StartTime = StartTimeBox.Text.Trim(), EndTime = EndTimeBox.Text.Trim(), RemoteType = "14", TimeType = "1",
        Category = ((ComboBoxItem)CategoryBox.SelectedItem).Content.ToString() ?? "SNS"
    };

    private void FillCourse(Course c)
    {
        _selectedCourseId = c.Id; YearBox.Text = c.Year.ToString(); InstitutionBox.Text = c.Institution; CourseNameBox.Text = c.Name; CourseCodeBox.Text = c.CourseCode;
        RoundBox.Text = c.Round.ToString(); StartDateBox.Text = c.StartDate.ToString("yyyy-MM-dd"); EndDateBox.Text = c.EndDate.ToString("yyyy-MM-dd"); WeekdaysBox.Text = c.Weekdays;
        StartTimeBox.Text = c.StartTime; EndTimeBox.Text = c.EndTime;
        foreach (ComboBoxItem item in CategoryBox.Items) if ((item.Content?.ToString() ?? "") == c.Category) { CategoryBox.SelectedItem = item; break; }
    }

    private void CourseGrid_SelectionChanged(object sender, SelectionChangedEventArgs e) { if (CourseGrid.SelectedItem is Course c) FillCourse(c); }
    private void NewCourse_Click(object sender, RoutedEventArgs e) { _selectedCourseId = 0; CourseNameBox.Clear(); CourseCodeBox.Clear(); RoundBox.Text = "1"; }
    private void SaveCourse_Click(object sender, RoutedEventArgs e)
    {
        try { Database.SaveCourse(ReadCourseForm()); ReloadCourses(); MessageBox.Show("저장했습니다."); }
        catch (Exception ex) { MessageBox.Show(ex.Message, "저장 오류"); }
    }
    private void DeleteCourse_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCourseId == 0 || MessageBox.Show("과정과 모든 코드를 삭제합니까?", "삭제", MessageBoxButton.YesNo) != MessageBoxResult.Yes) return;
        Database.DeleteCourse(_selectedCourseId); _selectedCourseId = 0; ReloadCourses();
    }

    private Course? SelectedCodeCourse => CodeCourseBox.SelectedItem as Course;
    private void CodeCourseBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ReloadCodes();
    private void ReloadCodes()
    {
        if (SelectedCodeCourse is not { } c) return;
        var teachers = Database.GetTeachers(c.Id); var rooms = Database.GetRooms(c.Id);
        TeacherGrid.ItemsSource = teachers; RoomGrid.ItemsSource = rooms; SubjectGrid.ItemsSource = Database.GetSubjects(c.Id);
        SubjectTeacherBox.ItemsSource = teachers; SubjectRoomBox.ItemsSource = rooms;
    }

    private void AddTeacher_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedCodeCourse is not { } c) return;
        try { Database.AddTeacher(new(){CourseId=c.Id,TeacherName=TeacherNameBox.Text.Trim(),TeacherCode=TeacherCodeBox.Text.Trim(),IsDefault=TeacherDefaultBox.IsChecked==true}); TeacherNameBox.Clear();TeacherCodeBox.Clear();ReloadCodes(); }
        catch(Exception ex){MessageBox.Show(ex.Message);}
    }
    private void DeleteTeacher_Click(object sender, RoutedEventArgs e){if(TeacherGrid.SelectedItem is CourseTeacherCode x){try{Database.DeleteCode("teacher_codes",x.Id);ReloadCodes();}catch(Exception ex){MessageBox.Show("교과목에서 사용 중인 코드는 삭제할 수 없습니다.\n"+ex.Message);}}}
    private void AddRoom_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedCodeCourse is not { } c) return;
        try { Database.AddRoom(new(){CourseId=c.Id,RoomName=RoomNameBox.Text.Trim(),RoomCode=RoomCodeBox.Text.Trim(),IsDefault=RoomDefaultBox.IsChecked==true});RoomNameBox.Clear();RoomCodeBox.Clear();ReloadCodes(); }
        catch(Exception ex){MessageBox.Show(ex.Message);}
    }
    private void DeleteRoom_Click(object sender, RoutedEventArgs e){if(RoomGrid.SelectedItem is CourseRoomCode x){try{Database.DeleteCode("room_codes",x.Id);ReloadCodes();}catch(Exception ex){MessageBox.Show("교과목에서 사용 중인 코드는 삭제할 수 없습니다.\n"+ex.Message);}}}
    private void AddSubject_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedCodeCourse is not { } c || SubjectTeacherBox.SelectedItem is not CourseTeacherCode t || SubjectRoomBox.SelectedItem is not CourseRoomCode r) return;
        try { Database.AddSubject(new(){CourseId=c.Id,SortOrder=int.Parse(SubjectOrderBox.Text),SubjectName=SubjectNameBox.Text.Trim(),SubjectCode=SubjectCodeBox.Text.Trim(),AllocatedHours=int.Parse(SubjectHoursBox.Text),TeacherCodeId=t.Id,RoomCodeId=r.Id});SubjectNameBox.Clear();SubjectCodeBox.Clear();ReloadCodes(); }
        catch(Exception ex){MessageBox.Show(ex.Message);}
    }
    private void DeleteSubject_Click(object sender, RoutedEventArgs e){if(SubjectGrid.SelectedItem is CourseSubjectCode x){Database.DeleteCode("subject_codes",x.Id);ReloadCodes();}}

    private void ValidateSchedule_Click(object sender, RoutedEventArgs e)
    {
        if(ScheduleCourseBox.SelectedItem is not Course c)return;
        try
        {
            var rows=ExportService.BuildSchedule(c,Database.GetSubjects(c.Id));
            SchedulePreview.Text=string.Join(Environment.NewLine,rows.Take(500).Select(x=>$"{x.TrainingDate}\t{x.SlotStart}\t{x.TeacherCode}\t{x.RoomCode}\t{x.SubjectCode}"));
            MessageBox.Show($"정상입니다. {rows.Count}시간, {rows.Select(x=>x.TrainingDate).Distinct().Count()}일");
        }
        catch(Exception ex){MessageBox.Show(ex.Message,"검사 실패");}
    }

    private void ExportSchedule_Click(object sender, RoutedEventArgs e)
    {
        if(ScheduleCourseBox.SelectedItem is not Course c)return;
        var path=SavePath($"{c.Year}_{Safe(c.Name)}_{c.Round}회_시간표.xlsx","Excel (*.xlsx)|*.xlsx");if(path is null)return;
        try{ExportService.ExportScheduleExcel(path,c,Database.GetSubjects(c.Id));MessageBox.Show("생성했습니다.\n"+path);}catch(Exception ex){MessageBox.Show(ex.Message);}
    }
    private void ExportDiaryExcel_Click(object sender, RoutedEventArgs e)
    {
        if(DiaryCourseBox.SelectedItem is not Course c)return;var teacher=DefaultTeacher(c.Id);var path=SavePath($"{c.Year}_{Safe(c.Name)}_{c.Round}회_훈련일지.xlsx","Excel (*.xlsx)|*.xlsx");if(path is null)return;
        try{ExportService.ExportDiaryExcel(path,c,teacher);MessageBox.Show("생성했습니다.\n"+path);}catch(Exception ex){MessageBox.Show(ex.Message);}
    }
    private void ExportDiaryPdf_Click(object sender, RoutedEventArgs e)
    {
        if(DiaryCourseBox.SelectedItem is not Course c)return;var teacher=DefaultTeacher(c.Id);var path=SavePath($"{c.Year}_{Safe(c.Name)}_{c.Round}회_훈련일지.pdf","PDF (*.pdf)|*.pdf");if(path is null)return;
        try{ExportService.ExportDiaryPdf(path,c,teacher);MessageBox.Show("생성했습니다.\n"+path);}catch(Exception ex){MessageBox.Show(ex.Message);}
    }

    private static string DefaultTeacher(long courseId)=>Database.GetTeachers(courseId).FirstOrDefault(x=>x.IsDefault)?.TeacherName??Database.GetTeachers(courseId).FirstOrDefault()?.TeacherName??"";
    private static string? SavePath(string name,string filter){var d=new SaveFileDialog{FileName=name,Filter=filter};return d.ShowDialog()==true?d.FileName:null;}
    private static string Safe(string value)=>string.Concat(value.Where(ch=>!Path.GetInvalidFileNameChars().Contains(ch))).Replace(' ','_');
}
