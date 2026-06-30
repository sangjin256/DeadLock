# DeadLock 비주얼 방향

## 사용 가능한 도구

- FEEL: `Assets/Feel`
- DOTween: `Assets/Plugins/Demigiant/DOTween`
- Shapes: `Assets/Shapes`
- UniTask: `com.cysharp.unitask`

## 방향

- 현재 원/사각형 비주얼 언어는 가독성과 feel을 개선할 수 있다면 변경한다.
- 확장 가능하고 선명한 절차적 노드와 연결에는 Shapes primitive를 검토한다.
- 물방울, 젤, 표면장력 느낌의 연결 비주얼을 탐색한다.
- 폴리싱보다 게임 상태 가독성을 먼저 지킨다.

## 경계

비주얼 효과는 View, Presenter, effect component, feedback presenter에 둔다. Domain과 Manager는 비주얼 패키지를 참조하지 않는다.

