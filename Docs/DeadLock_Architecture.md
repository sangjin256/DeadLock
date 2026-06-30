# DeadLock 아키텍처

## 목표

실용적 DDD와 Passive View MVP를 사용한다.

```text
01.Domain
02.Repository
03.Manager
04.UI
05.Bootstrap
06.Infrastructure
```

## 계층 의도

- Domain: 보드, 프로세스 노드, 리소스 노드, 연결, 리소스 Rule, 시뮬레이션.
- Repository: 저장/로드와 지속 플레이어 상태.
- Manager: Application Service 유스케이스와 DTO 캐시.
- UI: View, Presenter, 비주얼 피드백, DOTween, FEEL, Shapes, shader.
- Bootstrap: 씬 연결과 구체 객체 생성.
- Infrastructure: Steam, Android, iOS, Null 플랫폼 구현과 외부 저장소 구현.

## 중요한 경계

Manager가 Application 계층이다. Presenter는 프레젠테이션 어댑터다.

플랫폼 인터페이스는 Manager가 의존하는 포트로 둘 수 있고, 구체 구현은 Infrastructure에 둔다.

```text
03.Manager/Platform/IPlatformServices.cs
06.Infrastructure/Platform/SteamPlatformServices.cs
06.Infrastructure/Platform/AndroidPlatformServices.cs
06.Infrastructure/Platform/IOSPlatformServices.cs
06.Infrastructure/Platform/NullPlatformServices.cs
```

## 마이그레이션 메모

기존 코드는 현재 `Assets/scripts` 아래에 있다. 새 아키텍처 코드는 점진적으로 도입하고, 동작 보존을 우선한다.

## 검증 정책

Unity 검증은 수동 전용이다. 명시 요청 없이는 리컴파일이나 테스트를 실행하지 않는다.

