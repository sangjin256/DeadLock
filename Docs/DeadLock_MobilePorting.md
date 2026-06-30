# DeadLock 모바일 이식

## 목표

- 터치 우선 조작을 지원한다.
- desktop-only 입력 가정을 제거한다.
- 공유 게임 흐름에서 직접 Steam 의존성을 제거한다.
- 업적, cloud/local save, callback 처리는 플랫폼 추상화로 감싼다.

## 현재 위험

- Legacy 코드가 Steamworks를 직접 호출한다.
- Legacy 코드에 키보드와 마우스 중심 입력 경로가 있다.
- Legacy 코드가 desktop-style 해상도 동작을 강제한다.

## 목표 방향

- Steam, Android, iOS, Null 플랫폼 서비스를 `06.Infrastructure`에 둔다.
- Manager는 `IPlatformServices` 같은 포트에만 의존한다.
- Domain은 플랫폼 코드와 독립적이어야 한다.
- 모바일 입력 처리는 UI/View/Presenter 계층에서 담당한다.
- 향후 저장 마이그레이션을 위해 binary formatter 기반 저장보다 JSON 또는 versioned save data를 사용한다.

