# DesktopSchedule

C#과 WPF로 개발한 Windows 데스크톱 일정 및 할 일 관리 애플리케이션입니다.

## Download

**Windows x64**

[DesktopSchedule-Setup.exe 다운로드](https://github.com/ChoDoGyu/DesktopSchedule/releases/latest/download/DesktopSchedule-Setup.exe)

별도의 .NET Runtime 설치 없이 사용할 수 있는 Self-contained 방식으로 배포합니다.

> 현재 설치 파일에는 코드 서명 인증서가 적용되어 있지 않아 Windows SmartScreen 경고가 표시될 수 있습니다.

---

## Overview

DesktopSchedule은 Windows에서 장시간 실행해 두고 사용할 수 있도록 만든 로컬 일정 관리 프로그램입니다.

월간, 주간, 일간 화면에서 일정을 확인하고 관리할 수 있으며, 단순한 일정 CRUD를 넘어 일정 드래그 이동, 완료 관리, 알림, 시스템 트레이, Windows 자동 실행, 창 상태 저장 등 실제 데스크톱 애플리케이션에서 필요한 기능을 구현했습니다.

일정과 설정 데이터는 외부 서비스나 클라우드가 아닌 사용자의 PC에 저장됩니다.

### 주요 목표

- 월간 / 주간 / 일간 일정 관리
- 일정과 할 일의 생성, 수정, 삭제 및 완료 처리
- 화면에서 직접 조작할 수 있는 Drag & Drop 일정 이동
- 일정 알림 및 백그라운드 실행
- Windows 시작 시 자동 실행
- 장시간 실행을 고려한 데이터 조회 범위 및 리소스 관리
- 설치 프로그램을 통한 실제 Windows 애플리케이션 배포
- 프로그램 설치 파일과 사용자 데이터의 분리

---

## Features

### Calendar

- 월간 달력
- 주간 일정
- 일간 일정
- 이전 / 오늘 / 다음 날짜 이동
- 당일 일정 표시
- 여러 날짜에 걸친 일정 표시
- 하루 종일 일정 지원
- 일정 겹침을 고려한 화면 배치

### Schedule Management

- 일정 생성
- 일정 수정
- 일정 삭제
- 일정 완료 / 완료 취소
- 제목 및 상세 내용
- 시작 / 종료 날짜와 시간
- 하루 종일 일정
- 여러 날짜에 걸친 일정
- 할 일 / 완료한 일 구분

### Drag & Drop

- 주간 일정 Drag & Drop 이동
- 월간 일정 Drag & Drop 이동
- 일간 일정 Drag & Drop 이동
- 할 일을 완료 영역으로 Drag & Drop
- 드래그 중 일정 카드 Preview 표시
- 각 화면의 시간 단위에 맞는 이동 처리

### Reminder

- 일정별 알림 활성화
- 일정 시작 전 알림 시간 설정
- 알림 Popup 표시
- 알림 사운드
- 프로그램 실행 중 백그라운드 알림 검사
- 동일 알림의 중복 발생 방지
- 장시간 절전 또는 중단 이후 지나치게 오래된 알림 제외

### Desktop Application

- 시스템 트레이 아이콘
- 창 닫기 시 프로그램 종료 대신 트레이로 유지
- 트레이에서 창 열기 / 숨기기
- 트레이에서 프로그램 완전 종료
- Windows 로그인 시 자동 실행 설정
- Always On Top 설정
- Windows 작업 표시줄 표시 여부 설정
- 마지막 창 위치 및 크기 저장
- 다중 모니터 환경에서 유효하지 않은 창 위치 자동 복구

---

## Tech Stack

| Category | Technology |
| --- | --- |
| Language | C# |
| Framework | .NET 9 |
| UI | WPF |
| Architecture | MVVM |
| Database | SQLite |
| SQLite Provider | Microsoft.Data.Sqlite |
| Platform | Windows |
| Installer | Inno Setup |
| Version Control | Git / GitHub |

---

## Architecture

DesktopSchedule은 UI, 상태 관리, 데이터 접근 및 Windows 기능의 책임을 분리하도록 구성했습니다.

```text
DesktopSchedule
├─ Commands
│  └─ MVVM Command
│
├─ Controls
│  ├─ Calendar Layout
│  ├─ Daily Timeline
│  ├─ Drag Controller
│  ├─ Drag Preview
│  └─ Date / Time Controls
│
├─ Models
│  ├─ ScheduleItem
│  └─ AppSettings
│
├─ Repositories
│  ├─ IScheduleRepository
│  └─ SqliteScheduleRepository
│
├─ Services
│  ├─ DatabaseService
│  ├─ ScheduleService
│  ├─ ReminderScheduler
│  ├─ StartupService
│  ├─ TrayIconService
│  ├─ AppSettingsService
│  └─ WindowPlacementService
│
├─ Utilities
│  ├─ Calendar Calculation
│  ├─ Timeline Calculation
│  └─ Reminder Calculation
│
├─ ViewModels
│  ├─ Monthly
│  ├─ Weekly
│  ├─ Daily
│  ├─ Schedule Editor
│  └─ Settings
│
└─ Views
   ├─ Monthly
   ├─ Weekly
   ├─ Daily
   ├─ Schedule Editor
   ├─ Reminder Popup
   └─ Settings
```

### MVVM

View는 화면 표현을 담당하고, ViewModel은 화면 상태와 사용자 명령을 관리합니다.

일정 저장이나 Windows 기능과 같은 처리는 ViewModel에 직접 구현하지 않고 Repository와 Service 계층으로 분리했습니다.

### Repository

일정 데이터 접근은 `IScheduleRepository`를 기준으로 분리하고, 실제 저장은 `SqliteScheduleRepository`에서 담당합니다.

SQLite 연결, SQL 실행 및 `ScheduleItem` 변환 책임을 Repository 내부에 유지하여 화면 계층이 데이터베이스 구현에 직접 의존하지 않도록 구성했습니다.

### Services

기능별 책임을 Service로 분리했습니다.

- `DatabaseService` — SQLite 데이터베이스 생성 및 관리
- `ScheduleService` — 일정 관련 애플리케이션 로직
- `ReminderScheduler` — 주기적인 알림 대상 검사
- `StartupService` — Windows 시작 프로그램 등록 및 해제
- `TrayIconService` — 시스템 트레이 및 창 표시 상태 관리
- `AppSettingsService` — 애플리케이션 설정 저장
- `WindowPlacementService` — 창 위치 및 크기 저장 / 복원

---

## Data Storage

DesktopSchedule은 사용자 데이터를 프로그램 설치 경로와 분리합니다.

### Application

```text
%LocalAppData%\Programs\DesktopSchedule
```

설치된 실행 파일과 .NET Runtime을 포함한 애플리케이션 파일이 저장됩니다.

### User Data

```text
%LocalAppData%\DesktopSchedule
```

사용자 데이터는 이 경로에 별도로 저장됩니다.

주요 파일:

```text
desktop-schedule.db
app-settings.json
window-placement.json
```

- `desktop-schedule.db` — 일정 데이터
- `app-settings.json` — 애플리케이션 설정
- `window-placement.json` — 마지막 창 위치와 크기

프로그램을 제거하더라도 사용자 일정 데이터는 자동으로 삭제하지 않습니다.

따라서 DesktopSchedule을 제거한 뒤 다시 설치해도 기존 데이터가 남아 있다면 다시 사용할 수 있습니다.

---

## Performance & Stability

DesktopSchedule은 컴퓨터를 사용하는 동안 계속 실행될 수 있는 애플리케이션을 목표로 하기 때문에 장시간 실행과 데이터 증가를 고려했습니다.

### Screen Range Query

월간 / 주간 / 일간 화면을 열 때 모든 일정을 메모리로 불러온 뒤 필터링하지 않고, 현재 화면에 필요한 날짜 범위와 겹치는 일정만 SQLite에서 조회합니다.

이를 통해 일정 데이터가 증가하더라도 화면 표시를 위해 불필요한 전체 데이터를 반복해서 읽는 작업을 줄였습니다.

### Reminder Candidate Query

알림 검사 역시 전체 일정을 매번 조회하지 않습니다.

SQLite에서 다음 조건에 해당하는 일정만 먼저 조회합니다.

- 알림 활성화
- 미완료 일정
- 현재 알림 검사 시간 범위에 들어올 가능성이 있는 일정

조회된 후보에 대해서만 최종 알림 시각을 계산합니다.

### Reminder Safety

- 동일 실행 중 같은 알림 중복 발생 방지
- 시스템 시간이 변경된 경우 잘못된 역방향 검사 방지
- 장시간 절전 후 너무 오래 지난 알림 제외
- 개별 일정 데이터 오류가 다른 일정의 알림 검사를 중단시키지 않도록 처리

### Resource Management

SQLite Connection, Command, Reader 및 트레이 관련 리소스는 사용이 끝난 후 명시적으로 정리하도록 구성했습니다.

### Load Validation

대량 데이터 환경을 확인하기 위해 **50,000개의 일정 데이터**를 사용한 부하 검증을 진행했습니다.

화면 범위 조회와 알림 후보 조회가 전체 데이터 수 증가에 따라 불필요한 전체 조회를 반복하지 않는지 확인했습니다.

---

## Windows Integration

### Startup

설정에서 다음 옵션을 활성화할 수 있습니다.

```text
Windows 시작 시 DesktopSchedule 자동 실행
```

현재 Windows 사용자의 다음 Registry 위치를 이용합니다.

```text
HKEY_CURRENT_USER
└─ Software
   └─ Microsoft
      └─ Windows
         └─ CurrentVersion
            └─ Run
```

관리자 권한 없이 현재 사용자에 대해서만 등록됩니다.

프로그램을 제거할 때 자동 실행 Registry 값도 함께 정리하도록 Installer에 제거 처리를 구성했습니다.

### System Tray

메인 창의 X 버튼은 애플리케이션을 즉시 종료하지 않고 창을 숨깁니다.

프로그램은 시스템 트레이에서 계속 실행되며 알림 기능도 유지됩니다.

완전히 종료하려면 시스템 트레이의 DesktopSchedule 메뉴에서 `종료`를 선택합니다.

---

## Deployment

DesktopSchedule v1.0.0은 다음 기준으로 배포합니다.

```text
Configuration : Release
Platform      : Windows x64
Target        : net9.0-windows
Runtime       : win-x64
Deployment    : Self-contained
Single File   : Off
Trim          : Off
ReadyToRun    : Off
Installer     : Inno Setup
```

Self-contained 방식이므로 대상 PC에 .NET 9 Runtime을 별도로 설치할 필요가 없습니다.

설치는 현재 Windows 사용자 범위에서 진행되며 관리자 권한을 요구하지 않습니다.

---

## Installation

1. 상단의 `DesktopSchedule-Setup.exe`를 다운로드합니다.
2. 설치 파일을 실행합니다.
3. 설치 경로를 확인합니다.
4. 필요하면 바탕화면 바로가기 생성을 선택합니다.
5. 설치를 완료한 뒤 DesktopSchedule을 실행합니다.

기본 설치 경로:

```text
%LocalAppData%\Programs\DesktopSchedule
```

사용자 데이터 경로:

```text
%LocalAppData%\DesktopSchedule
```

---

## Validation

v1.0.0 배포 전 다음 항목을 직접 검증했습니다.

- Release / Windows x64 Self-contained Publish
- Publish 결과물 직접 실행
- 일정 생성 / 수정 / 삭제 / 완료
- 프로그램 종료 후 일정 데이터 유지
- 알림 Popup 및 사운드
- 시스템 트레이 동작
- Windows 자동 시작 등록 / 해제
- Windows 로그인 후 자동 실행
- 설치 프로그램을 통한 설치
- 바탕화면 바로가기
- 프로그램 제거
- 제거 후 사용자 데이터 유지
- 재설치 후 기존 데이터 복원
- 제거 시 Windows 자동 시작 Registry 정리
- 50,000개 일정 데이터 부하 검증

---

## Project Scope

DesktopSchedule은 개인 PC에서 독립적으로 사용할 수 있는 로컬 일정 관리 프로그램을 목표로 합니다.

현재 버전에서는 Google Calendar 등의 외부 캘린더 서비스와 동기화하지 않습니다.

일정 및 설정 데이터는 사용자의 Windows 로컬 환경에서 관리합니다.

---

## Version

### v1.0.0

DesktopSchedule 최초 정식 배포 버전입니다.

---

## Developer

**Cho Do Gyu**