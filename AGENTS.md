# Narae Manager Agent Guide

- Preserve the existing .NET 8 + WPF + SQLite application structure.
- Do not replace the repository with a new project or remove existing features.
- Keep management data organized as 기관 → 연도 → 과정 → 회차.
- 강사코드, 강의실코드, 교과목코드는 전역 코드가 아니며 반드시 기관, 연도, 과정, 회차 범위에 종속시킨다.
- Existing SQLite databases must be extended without deleting user data.
