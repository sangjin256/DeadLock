# DeadLock 개발 현황

## 현재 상태

- 프로젝트 분석 결과 `GameManager`가 주요 legacy coordination hotspot으로 확인되었다.
- DDD/MVP 마이그레이션, 로컬 문서, 비주얼 피드백, 수동 Unity 검증을 지원하는 로컬 프로젝트 스킬을 등록했다.
- FEEL, DOTween, Shapes, UniTask는 프로젝트에 들어와 있으며 프레젠테이션/이펙트 경계에서만 사용한다.
- Steam, Android, iOS 같은 플랫폼 구현은 `06.Infrastructure`로 분리하는 방향으로 정했다.

## 다음 유력 작업

- 보드, 프로세스, 리소스, 연결, 리소스 Rule에 대한 순수 Domain 타입 추출.
- 게임 동작 변경 전 focused Domain 테스트 추가.
- 모바일 입력과 저장/플랫폼 추상화 계획 구체화.
- 기존 `GameManager` 책임을 Manager, Presenter, Infrastructure로 나누기.

## 검증

Unity 검증은 의도적으로 수동 전용이다. 검증 요청이 실행되지 않았다면 Unity 컴파일 성공을 가정하지 않는다.

