---
name: deadlock-visual-feedback
description: "DeadLock의 FEEL, DOTween, Shapes, shader, UniTask 기반 비주얼 피드백과 게임 feel 지침입니다. 노드 모양 변경, 연결 렌더링, 선 애니메이션, 물방울/젤 합쳐짐 효과, 성공/실패/대기 피드백, 햅틱, 파티클, 카메라/배경 피드백, 프레젠테이션 계층 비주얼 폴리싱 작업에 사용합니다."
---

# DeadLock 비주얼 피드백

## 사용 가능한 도구

현재 프로젝트에는 다음 도구가 들어 있다.

- FEEL: `Assets/Feel`
- Shapes: `Assets/Shapes`
- DOTween: `Assets/Plugins/Demigiant/DOTween`
- UniTask: `Packages/manifest.json`의 `com.cysharp.unitask`

이 도구들은 프레젠테이션과 이펙트에만 사용한다. Domain, Repository, Manager가 이 패키지들에 의존하게 만들지 않는다.

## 비주얼 방향

- 기존 원/사각형 언어는 바꿀 수 있다. 현재 SpriteRenderer 기반 원과 사각형을 최종 형태로 가정하지 않는다.
- 선명하고 확장 가능한 노드와 연결을 위해 Shapes의 `Disc`, `Rectangle`, `Line`, `Polyline` 같은 primitive를 검토한다.
- 퍼즐 상태의 가독성을 먼저 지키고, 그 다음 부드러움, 움직임, 연출감을 더한다.
- 연결 비주얼은 물방울, 젤, 표면장력, 액체가 합쳐지는 느낌을 목표로 할 수 있다.

## 권장 경계

- `ConnectionView`: 선/경로 렌더링, 이동하는 끝점, 물방울 합쳐짐 비주얼.
- `NodeView`: 프로세스/리소스 모양 렌더링, 색 상태, pulse, highlight, idle motion.
- `FeedbackPresenter`: Manager/Presenter 결과를 받아 FEEL, DOTween, Shapes 피드백을 트리거.
- `VisualSettings` 또는 ScriptableObject 설정: 색상, 두께, 애니메이션 시간, softness 값.

## 도구 사용 기준

- 정적 node sprite나 LineRenderer를 대체할 때 Shapes 기반 절차적 2D geometry를 우선 검토한다.
- 간단한 transform, fade, color, timing에는 DOTween을 사용한다.
- 성공, 실패, 대기, 연결, 잠금 해제, 햅틱, 카메라/배경 반응 같은 재사용 피드백에는 FEEL을 사용한다.
- Shapes와 DOTween만으로 merge, glow, soft edge를 표현하기 어렵다면 custom shader를 사용한다.
- 여러 await를 넘나들거나 씬/View 생명주기 취소가 필요한 비주얼 시퀀스에는 UniTask를 사용한다.

## 규칙

- 비주얼 컴포넌트에 게임 정답 판정을 넣지 않는다.
- 이펙트가 Domain 상태를 바꾸지 않게 한다.
- 애니메이션 시간과 색상 팔레트는 설정 가능하게 둔다.
- View disable 또는 Dispose 시 tween과 async sequence를 취소하거나 종료한다.
- 모바일 친화적인 이펙트를 우선한다. 전체 화면 RenderTexture 같은 비용 큰 효과는 명확한 성능 이유와 fallback이 있을 때만 사용한다.

## 검증 정책

Unity 컴파일이나 PlayMode 검증은 자동 실행하지 않는다. 검증이 요청되면 `deadlock-unity-validation-manual`을 사용한다.

