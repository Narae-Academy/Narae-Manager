using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace NaraeManager;

public partial class MainWindow : Window
{
    private long _instId, _yearId, _courseId, _roundId;
    private List<Institution> _institutions = [];
    private List<OperationYear> _years = [];
    private List<TrainingCourse> _trainingCourses = [];
    private List<CourseRound> _rounds = [];
    private List<Course> _courses = [];

    public MainWindow()
    {
        InitializeComponent();
        RoundStartDateBox.Text = DateTime.Today.ToString("yyyy-MM-dd");
        RoundEndDateBox.Text = DateTime.Today.ToString("yyyy-MM-dd");
        YearValueBox.Text = DateTime.Today.Year.ToString();
        ReloadAll();
    }

    private void ReloadAll()
    {
        _institutions = Database.GetInstitutions(); InstitutionGrid.ItemsSource = _institutions; YearInstitutionBox.ItemsSource = _institutions;
        _years = Database.GetYears(); YearGrid.ItemsSource = _years; CourseYearBox.ItemsSource = _years;
        _trainingCourses = Database.GetTrainingCourses(); TrainingCourseGrid.ItemsSource = _trainingCourses; RoundCourseBox.ItemsSource = _trainingCourses;
        _rounds = Database.GetRounds(); RoundGrid.ItemsSource = _rounds;
        _courses = Database.GetCourses(); CodeCourseBox.ItemsSource = _courses; ScheduleCourseBox.ItemsSource = _courses; DiaryCourseBox.ItemsSource = _courses;
    }

    private void InstitutionGrid_SelectionChanged(object s, SelectionChangedEventArgs e){if(InstitutionGrid.SelectedItem is not Institution x)return;_instId=x.Id;InstNameBox.Text=x.Name;InstBizBox.Text=x.BusinessNumber;InstAddressBox.Text=x.Address;InstPhoneBox.Text=x.Phone;InstEmailBox.Text=x.Email;InstRepBox.Text=x.RepresentativeName;InstActiveBox.IsChecked=x.IsActive;}
    private void NewInstitution_Click(object s,RoutedEventArgs e){_instId=0;InstNameBox.Clear();InstBizBox.Clear();InstAddressBox.Clear();InstPhoneBox.Clear();InstEmailBox.Clear();InstRepBox.Clear();InstActiveBox.IsChecked=true;}
    private void SaveInstitution_Click(object s,RoutedEventArgs e)=>Run("기관을 저장했습니다.",()=>{Database.SaveInstitution(new(){Id=_instId,Name=InstNameBox.Text.Trim(),BusinessNumber=InstBizBox.Text.Trim(),Address=InstAddressBox.Text.Trim(),Phone=InstPhoneBox.Text.Trim(),Email=InstEmailBox.Text.Trim(),RepresentativeName=InstRepBox.Text.Trim(),IsActive=InstActiveBox.IsChecked==true});ReloadAll();});
    private void DeleteInstitution_Click(object s,RoutedEventArgs e){if(_instId==0)return; if(Confirm("기관을 삭제합니까?"))Run("기관을 삭제했습니다.",()=>{Database.DeleteInstitution(_instId);_instId=0;ReloadAll();});}

    private void YearGrid_SelectionChanged(object s,SelectionChangedEventArgs e){if(YearGrid.SelectedItem is not OperationYear y)return;_yearId=y.Id;YearValueBox.Text=y.Year.ToString();YearInstitutionBox.SelectedItem=_institutions.FirstOrDefault(i=>i.Id==y.InstitutionId);}
    private void SaveYear_Click(object s,RoutedEventArgs e)=>Run("연도를 등록했습니다.",()=>{Database.SaveYear(new(){InstitutionId=(YearInstitutionBox.SelectedItem as Institution)?.Id??0,Year=int.Parse(YearValueBox.Text)});ReloadAll();});
    private void DeleteYear_Click(object s,RoutedEventArgs e){if(_yearId==0)return;if(Confirm("연도를 삭제합니까?"))Run("연도를 삭제했습니다.",()=>{Database.DeleteYear(_yearId);_yearId=0;ReloadAll();});}

    private void TrainingCourseGrid_SelectionChanged(object s,SelectionChangedEventArgs e){if(TrainingCourseGrid.SelectedItem is not TrainingCourse c)return;_courseId=c.Id;CourseYearBox.SelectedItem=_years.FirstOrDefault(y=>y.Id==c.OperationYearId);CourseNameBox.Text=c.Name;CourseCodeBox.Text=c.CourseCode;CourseHoursBox.Text=c.TotalTrainingHours.ToString();CourseActiveBox.IsChecked=c.IsActive;SelectCombo(CourseTypeBox,c.CourseType);}
    private void NewTrainingCourse_Click(object s,RoutedEventArgs e){_courseId=0;CourseNameBox.Clear();CourseCodeBox.Clear();CourseHoursBox.Text="0";CourseActiveBox.IsChecked=true;}
    private void SaveTrainingCourse_Click(object s,RoutedEventArgs e)=>Run("과정을 저장했습니다.",()=>{Database.SaveTrainingCourse(new(){Id=_courseId,OperationYearId=(CourseYearBox.SelectedItem as OperationYear)?.Id??0,Name=CourseNameBox.Text.Trim(),CourseCode=CourseCodeBox.Text.Trim(),CourseType=SelectedText(CourseTypeBox),TotalTrainingHours=int.Parse(CourseHoursBox.Text),IsActive=CourseActiveBox.IsChecked==true});ReloadAll();});
    private void DeleteTrainingCourse_Click(object s,RoutedEventArgs e){if(_courseId==0)return;if(Confirm("과정을 삭제합니까?"))Run("과정을 삭제했습니다.",()=>{Database.DeleteTrainingCourse(_courseId);_courseId=0;ReloadAll();});}

    private void RoundGrid_SelectionChanged(object s,SelectionChangedEventArgs e){if(RoundGrid.SelectedItem is not CourseRound r)return;_roundId=r.Id;RoundCourseBox.SelectedItem=_trainingCourses.FirstOrDefault(c=>c.Id==r.CourseId);RoundNoBox.Text=r.RoundNo.ToString();RoundStartDateBox.Text=r.StartDate.ToString("yyyy-MM-dd");RoundEndDateBox.Text=r.EndDate.ToString("yyyy-MM-dd");RoundWeekdaysBox.Text=r.Weekdays;RoundStartTimeBox.Text=r.StartTime;RoundEndTimeBox.Text=r.EndTime;RoundCapacityBox.Text=r.Capacity.ToString();RoundStatusBox.Text=r.Status;RoundHoursBox.Text=r.TotalTrainingHours.ToString();}
    private void NewRound_Click(object s,RoutedEventArgs e){_roundId=0;RoundNoBox.Text="1";RoundStartDateBox.Text=DateTime.Today.ToString("yyyy-MM-dd");RoundEndDateBox.Text=DateTime.Today.ToString("yyyy-MM-dd");RoundHoursBox.Text="0";}
    private void SaveRound_Click(object s,RoutedEventArgs e)=>Run("회차를 저장했습니다.",()=>{Database.SaveRound(new(){Id=_roundId,CourseId=(RoundCourseBox.SelectedItem as TrainingCourse)?.Id??0,RoundNo=int.Parse(RoundNoBox.Text),StartDate=DateTime.Parse(RoundStartDateBox.Text),EndDate=DateTime.Parse(RoundEndDateBox.Text),Weekdays=RoundWeekdaysBox.Text.Trim(),StartTime=RoundStartTimeBox.Text.Trim(),EndTime=RoundEndTimeBox.Text.Trim(),Capacity=int.Parse(RoundCapacityBox.Text),Status=RoundStatusBox.Text.Trim(),TotalTrainingHours=int.Parse(RoundHoursBox.Text)});ReloadAll();});
    private void DeleteRound_Click(object s,RoutedEventArgs e){if(_roundId==0)return;if(Confirm("회차를 삭제합니까?"))Run("회차를 삭제했습니다.",()=>{Database.DeleteRound(_roundId);_roundId=0;ReloadAll();});}

    private Course? SelectedCodeCourse => CodeCourseBox.SelectedItem as Course;
    private void CodeCourseBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => ReloadCodes();
    private void ReloadCodes(){if(SelectedCodeCourse is not { } c)return;var teachers=Database.GetTeachers(c.Id);var rooms=Database.GetRooms(c.Id);TeacherGrid.ItemsSource=teachers;RoomGrid.ItemsSource=rooms;SubjectGrid.ItemsSource=Database.GetSubjects(c.Id);SubjectTeacherBox.ItemsSource=teachers;SubjectRoomBox.ItemsSource=rooms;}
    private void AddTeacher_Click(object sender,RoutedEventArgs e){if(SelectedCodeCourse is not { } c)return;Run("강사 코드를 추가했습니다.",()=>{Database.AddTeacher(new(){CourseId=c.Id,RoundId=c.Id,TeacherName=TeacherNameBox.Text.Trim(),TeacherCode=TeacherCodeBox.Text.Trim(),IsDefault=TeacherDefaultBox.IsChecked==true});TeacherNameBox.Clear();TeacherCodeBox.Clear();ReloadCodes();});}
    private void DeleteTeacher_Click(object sender,RoutedEventArgs e){if(TeacherGrid.SelectedItem is CourseTeacherCode x)Run("강사 코드를 삭제했습니다.",()=>{Database.DeleteCode("teacher_codes",x.Id);ReloadCodes();});}
    private void AddRoom_Click(object sender,RoutedEventArgs e){if(SelectedCodeCourse is not { } c)return;Run("강의실 코드를 추가했습니다.",()=>{Database.AddRoom(new(){CourseId=c.Id,RoundId=c.Id,RoomName=RoomNameBox.Text.Trim(),RoomCode=RoomCodeBox.Text.Trim(),IsDefault=RoomDefaultBox.IsChecked==true});RoomNameBox.Clear();RoomCodeBox.Clear();ReloadCodes();});}
    private void DeleteRoom_Click(object sender,RoutedEventArgs e){if(RoomGrid.SelectedItem is CourseRoomCode x)Run("강의실 코드를 삭제했습니다.",()=>{Database.DeleteCode("room_codes",x.Id);ReloadCodes();});}
    private void AddSubject_Click(object sender,RoutedEventArgs e){if(SelectedCodeCourse is not { } c||SubjectTeacherBox.SelectedItem is not CourseTeacherCode t||SubjectRoomBox.SelectedItem is not CourseRoomCode r)return;Run("교과목 코드를 추가했습니다.",()=>{Database.AddSubject(new(){CourseId=c.Id,RoundId=c.Id,SortOrder=int.Parse(SubjectOrderBox.Text),SubjectName=SubjectNameBox.Text.Trim(),SubjectCode=SubjectCodeBox.Text.Trim(),AllocatedHours=int.Parse(SubjectHoursBox.Text),TeacherCodeId=t.Id,RoomCodeId=r.Id});SubjectNameBox.Clear();SubjectCodeBox.Clear();ReloadCodes();});}
    private void DeleteSubject_Click(object sender,RoutedEventArgs e){if(SubjectGrid.SelectedItem is CourseSubjectCode x)Run("교과목 코드를 삭제했습니다.",()=>{Database.DeleteCode("subject_codes",x.Id);ReloadCodes();});}

    private void ValidateSchedule_Click(object sender,RoutedEventArgs e){if(ScheduleCourseBox.SelectedItem is not Course c)return;Run($"정상입니다.",()=>{var rows=ExportService.BuildSchedule(c,Database.GetSubjects(c.Id));SchedulePreview.Text=string.Join(Environment.NewLine,rows.Take(500).Select(x=>$"{x.TrainingDate}\t{x.SlotStart}\t{x.TeacherCode}\t{x.RoomCode}\t{x.SubjectCode}"));MessageBox.Show($"정상입니다. {rows.Count}시간, {rows.Select(x=>x.TrainingDate).Distinct().Count()}일");});}
    private void ExportSchedule_Click(object sender,RoutedEventArgs e){if(ScheduleCourseBox.SelectedItem is not Course c)return;var path=SavePath($"{c.Year}_{Safe(c.Name)}_{c.Round}회_시간표.xlsx","Excel (*.xlsx)|*.xlsx");if(path is null)return;Run("고용24 엑셀을 생성했습니다.",()=>ExportService.ExportScheduleExcel(path,c,Database.GetSubjects(c.Id)));}
    private void ExportDiaryExcel_Click(object sender,RoutedEventArgs e){if(DiaryCourseBox.SelectedItem is not Course c)return;var path=SavePath($"{c.Year}_{Safe(c.Name)}_{c.Round}회_훈련일지.xlsx","Excel (*.xlsx)|*.xlsx");if(path is null)return;Run("훈련일지 Excel을 생성했습니다.",()=>ExportService.ExportDiaryExcel(path,c,DefaultTeacher(c.Id)));}
    private void ExportDiaryPdf_Click(object sender,RoutedEventArgs e){if(DiaryCourseBox.SelectedItem is not Course c)return;var path=SavePath($"{c.Year}_{Safe(c.Name)}_{c.Round}회_훈련일지.pdf","PDF (*.pdf)|*.pdf");if(path is null)return;Run("훈련일지 PDF를 생성했습니다.",()=>ExportService.ExportDiaryPdf(path,c,DefaultTeacher(c.Id)));}

    private static void Run(string ok,Action action){try{action();MessageBox.Show(ok);}catch(Exception ex){MessageBox.Show(ex.Message,"오류");}}
    private static bool Confirm(string text)=>MessageBox.Show(text,"확인",MessageBoxButton.YesNo)==MessageBoxResult.Yes;
    private static string SelectedText(ComboBox b)=>((ComboBoxItem)b.SelectedItem).Content?.ToString()??"";
    private static void SelectCombo(ComboBox b,string text){foreach(ComboBoxItem i in b.Items)if((i.Content?.ToString()??"")==text){b.SelectedItem=i;break;}}
    private static string DefaultTeacher(long courseId)=>Database.GetTeachers(courseId).FirstOrDefault(x=>x.IsDefault)?.TeacherName??Database.GetTeachers(courseId).FirstOrDefault()?.TeacherName??"";
    private static string? SavePath(string name,string filter){var d=new SaveFileDialog{FileName=name,Filter=filter};return d.ShowDialog()==true?d.FileName:null;}
    private static string Safe(string value)=>string.Concat(value.Where(ch=>!System.IO.Path.GetInvalidFileNameChars().Contains(ch))).Replace(' ','_');
}
