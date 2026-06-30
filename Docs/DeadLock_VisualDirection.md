# DeadLock 비주얼 기획서

이 문서는 Shapes 기반 비주얼 프로토타입에서 확정한 방향을 실제 인게임 `ProcessView`, `ResourceView`, `ConnectionView`, `FeedbackPresenter`로 옮기기 위한 기준 문서다.

현재 기준 구현은 `Assets/scripts/VisualPrototype/DeadLockShapesHierarchyPrototype.cs`이다. 아래 수치는 프로토타입 기준값이며, 실제 게임 카메라 스케일과 모바일 해상도에서 약간 조정할 수 있다.

## 핵심 방향

- 전체 인상은 미니멀하고 모던한 Mini Metro 계열의 선명한 퍼즐 보드로 간다.
- 게임 상태의 가독성을 최우선으로 두고, 그 다음에 부드러운 연결감과 물방울/젤 느낌을 더한다.
- 프로세스는 모두 원형, 리소스는 모두 둥근 사각형으로 고정한다.
- 프로세스 본체색은 규칙 색이 아니라 장식/선택/상태 표현이다.
- 실제 퍼즐 색은 프로세스 위의 필요 색 칩과 리소스 색이다.
- 색을 새 규칙에 과하게 추가하지 않는다. 새 퍼즐은 가능한 한 패턴, 위치, 라인, 아이콘, 모션으로 구분한다.

## 사용 가능한 도구

- FEEL: `Assets/Feel`
- DOTween: `Assets/Plugins/Demigiant/DOTween`
- Shapes: `Assets/Shapes`
- UniTask: `com.cysharp.unitask`
- Shader: 연결선의 물방울/젤/표면장력 표현이 Shapes만으로 부족할 때 사용한다.

## 아키텍처 경계

- 비주얼은 `Presentation`, `View`, `Presenter`, `Effect`, `FeedbackPresenter`에 둔다.
- `Domain`, `Repository`, `Manager/Application Service`는 FEEL, DOTween, Shapes, Shader, Unity View 구현에 의존하지 않는다.
- 게임 정답 판정은 Domain/Rule이 담당하고, View는 전달받은 상태를 표현한다.
- DOTween/FEEL/UniTask 시퀀스는 View disable, scene unload, presenter dispose 때 취소되어야 한다.

## 색상 팔레트 기준

프로토타입 기준 색상:

- Blue: `(0.42, 0.61, 0.95, 1)`
- Green: `(0.72, 0.82, 0.20, 1)`
- Red: `(0.93, 0.36, 0.36, 1)`
- Mint: `(0.18, 0.78, 0.68, 1)`
- Amber: `(0.98, 0.68, 0.20, 1)`
- Ink: `(0.055, 0.065, 0.075, 1)`
- Station Fill: `(0.91, 0.94, 0.95, 1)`
- Muted: `(0.72, 0.78, 0.80, 1)`

배경은 프로토타입에서는 어두운 색을 사용하지만, 실제 게임은 밝은 배경도 고려한다. 따라서 Relay 라인처럼 배경 위에 직접 올라가는 보조 라인은 밝은 색보다 어두운 중립색을 우선한다.

## Process 비주얼

프로세스는 원형 노드다.

- 그림자: 원형, 위치 `(0.07, -0.07)`, 반지름 `0.70`, 검정 alpha `0.26`
- 외곽 stroke: 원형, 반지름 `0.68`
- 내부 fill: 원형, 반지름 `0.60`, 밝은 station fill
- 중앙 포트: 검정 원, 반지름 `0.16`
- 별도 텍스트 라벨은 표시하지 않는다.

프로세스는 리소스와 달리 규칙 종류를 직접 나타내지 않는다. 프로세스가 요구하는 색은 상단의 필요 색 트레이에 표시한다.

### 필요 색 트레이

프로세스 위의 필요 색 칩은 배경색에 묻히지 않도록 어두운 배경판을 가진다.

- 위치: 프로세스 중심 기준 `(0, 0.88)`
- tray width: `max(0.46, 0.28 + 색 개수 * 0.28)`
- tray height: `0.32`
- tray corner radius: `0.16`
- tray shadow: `(0.035, -0.035)`, 검정 alpha `0.28`
- tray background: Ink alpha `0.78`
- 색 칩 rim: 흰색 alpha `0.82`, 반지름 `0.125`
- 색 칩 fill: 요구 색, 반지름 `0.10`
- 색 칩 간격: `0.32`

## Resource 공통 비주얼

리소스는 모두 둥근 사각형 노드다. 리소스 종류는 외곽 형태가 아니라 내부 아이콘/슬롯/상태 표현으로 구분한다.

- 그림자: rounded rectangle, 위치 `(0.07, -0.07)`, 크기 `(1.02, 1.02)`, corner `0.21`, 검정 alpha `0.25`
- 외곽 stroke: rounded rectangle, 크기 `(0.98, 0.98)`, corner `0.20`, 리소스 대표색
- 내부 fill: rounded rectangle, 크기 `(0.83, 0.83)`, corner `0.17`, station fill
- Clock의 남은 턴 숫자를 제외하면 리소스 타입 텍스트는 표시하지 않는다.

외곽 stroke는 Shapes stroke가 아니라 채워진 사각형을 뒤에 깔고, 그 위에 내부 fill을 올리는 구조를 사용한다. 이렇게 해야 모서리에 빈칸이 보이지 않는다.

## Resource 타입별 아이콘

### Basic

기본 색 리소스다.

- 중앙 포트: 검정 원, 반지름 `0.16`
- 의미: 색이 맞으면 연결 가능한 기본 리소스

### Capacity

동시에 여러 프로세스를 받을 수 있는 리소스다.

- 내부 슬롯: 원형 슬롯
- 슬롯 반지름: `0.11`
- 1개: 중앙에 배치
- 2개: 좌우 한 줄 배치
- 3개: ColorSwitch/Simultaneous와 같은 방사형 삼각형 배치와 y `-0.06` 보정을 사용한다.
- 4개: 2x2 바둑판 형태로 배치
- 5개 이상: 원형 배치 fallback을 사용한다.
- 사용 가능 슬롯: 리소스 색으로 채움
- 비어 있거나 아직 미사용 슬롯: muted alpha `0.42`
- 별도 `2/3` 텍스트는 표시하지 않고 슬롯 개수와 채움 상태로 읽히게 한다.

### Simultaneous

필요한 연결이 모두 동시에 충족되어야 작동하는 리소스다.

- 내부 슬롯: 원형 슬롯 3개
- 슬롯 배치: ColorSwitch처럼 중앙 허브를 기준으로 원형 배치한다.
- 슬롯 배치 반경: `0.29`
- 3개 슬롯은 삼각형 형태로 보이게 배치한다.
- 3개 슬롯일 때는 시각적 균형과 동일한 link 길이를 위해 슬롯과 중앙 허브 그룹에 y `-0.06` 보정을 적용한다.
- 중앙 허브: 원형 activation light, 반지름 `0.12`
- 슬롯과 허브는 얇은 선으로 연결한다.
- 연결선 두께: `0.04`
- 연결선 색: 리소스 색 alpha `0.62`
- 모든 입력이 들어오기 전: 중앙 허브는 red
- 모든 슬롯이 동시에 충족된 후: 중앙 허브가 green으로 바뀐다.

이 리소스는 `+`, 글자, 추상 아이콘보다 "여러 슬롯이 한 중앙 허브에 동시에 연결되는 그림"이 더 직관적이다.

### ColorSwitch

사용 후 또는 상태 변화에 따라 현재 색이 순환/변경되는 리소스다.

- 내부 색 점: 여러 개의 작은 원
- 점 반지름: `0.095`
- 점 배치: 중심에서 거리 `0.23`, 원형 배치
- 3개 색 점일 때는 Simultaneous와 같은 y `-0.06` 삼각형 보정을 적용한다.
- 의미: 현재 또는 순환 가능한 리소스 색 후보를 보여준다.

프로세스 본체색과 혼동되지 않게, ColorSwitch의 색은 오직 리소스가 제공/변경하는 실제 퍼즐 색을 뜻한다.

### EmptyColor

처음 들어온 색으로 고정되는 리소스다.

- 현재 placeholder: muted X 표시
- slash 위치: `(-0.19, -0.19)` to `(0.19, 0.19)`, `(-0.19, 0.19)` to `(0.19, -0.19)`
- slash 두께: `0.06`

추후 개선 후보:

- X보다 "비어 있는 후보" 느낌이 강한 점선 원, 빈 물방울, 빈 슬롯 표현을 검토한다.
- 첫 색이 들어오면 내부 fill 또는 작은 물방울이 해당 색으로 부드럽게 채워지는 모션을 사용한다.

### Clock

남은 턴 수가 핵심인 리소스다.

- 중앙 숫자를 최우선으로 읽히게 한다.
- 숫자 font size: `5`
- 숫자 위치: `(0, 0)`
- 숫자 색: Ink
- 보조 시계 링: 반지름 `0.31`, 두께 `0.035`, 리소스 색 alpha `0.48`
- 보조 시계 바늘: 두께 `0.035`, 리소스 색 alpha `0.32`

우상단 작은 배지는 모바일에서 작게 보일 수 있으므로, 현재 방향은 중앙 숫자 우선이다.

## 일반 연결선

프로세스와 리소스를 잇는 기본 연결선은 Mini Metro처럼 한 줄로 단순하게 보인다.

- 라인 두께: `0.15`
- end cap: round
- 그림자 두께: `0.23`
- 그림자 색: 검정 alpha `0.34`
- 실제 라인 색: 연결되는 리소스/요구 색
- 선은 가능한 한 직접적인 직선으로 보여준다.
- 연결선 중간에 불필요한 원이나 장식은 넣지 않는다.

추후 고급 표현:

- 연결이 생성될 때 끝점에 작은 물방울이 생기고, 노드/라인과 합쳐지는 느낌을 준다.
- Shader 또는 별도 effect mesh로 젤/표면장력/메타볼 느낌을 검토한다.
- 단, 퍼즐 가독성을 해치거나 모바일 성능 부담이 크면 Shapes 기반 선 + DOTween scale/fade로 대체한다.

## Relay 공간 퍼즐

Relay는 기본 리소스 종류가 아니라 기존 리소스 위에 얹히는 공간 퍼즐 모디파이어다.

Relay 대상 리소스들은 서로 떨어져 있어도 같은 쌍이라는 것을 라인으로 보여준다. 기본 상태에서는 보드 중앙 시야를 가리지 않기 위해 전체 선을 그리지 않고, 각 리소스에서 일정 길이만 나오는 스텁 라인을 사용한다.

공통 기준:

- Relay 라인 그룹은 일반 연결선과 분리한다.
- Relay stub length: `0.60`
- node padding: `0.52`
- stub line thickness: `0.14`
- stub line color: 어두운 중립색 `(0.08, 0.09, 0.10, alpha 0.45)`
- end cap: round
- 끝 장식은 현재 Shapes 원 placeholder를 사용하고, 추후 전용 스프라이트로 교체한다.

### Relay Link

의미: 연결된 두 리소스가 공유 상태를 가진다. 한쪽을 사용 중이면 다른쪽이 영향을 받거나 잠긴다.

비주얼:

- 각 스텁 끝에 같은 고리형 원 placeholder
- placeholder ring radius: `0.12`
- placeholder ring thickness: `0.035`
- 양쪽 장식은 동일해야 "묶인 쌍"으로 읽힌다.

추후 피드백:

- 한쪽이 사용 중이면 반대쪽 스텁과 장식이 어두워지거나 잠금 아이콘으로 전환된다.
- 연결된 두 리소스가 동시에 약하게 pulse한다.

### Relay Transfer

의미: 한쪽 리소스의 색/상태가 다른쪽으로 전달된다.

비주얼:

- 송신 쪽 스텁 끝: 채워진 원 placeholder
- 송신 원 radius: `0.095`
- 수신 쪽 스텁 끝: 고리형 원 placeholder
- 수신 ring radius: `0.12`
- 수신 ring thickness: `0.035`
- 방향 표시: 송신 스텁 위 작은 점 placeholder
- 방향 점 radius: `0.055`
- 방향 점 위치: 송신 스텁의 약 `62%` 지점

추후 피드백:

- 활성화 시 방향 점 또는 작은 물방울이 송신 쪽에서 수신 쪽으로 이동한다.
- 전달된 색이 수신 리소스 내부에 부드럽게 채워진다.
- 색을 추가 설명으로 쓰기보다, "움직임"으로 Transfer임을 보여준다.

### Relay hover / 선택 상태

릴레이로 연결된 두 리소스 중 하나에 마우스를 올리거나, 모바일에서 탭/선택하면 두 리소스를 잇는 전체 선이 보인다.

- 기본 상태: 양쪽 stub만 표시
- hover/선택 진입: 전체 Relay line fade in
- hover/선택 해제: 전체 Relay line fade out
- fade 시간 후보: `0.12s ~ 0.20s`
- 전체 라인은 stub보다 alpha를 높이고, 필요하면 두께를 약간 키운다.
- Link 전체 라인: 단순 실선
- Transfer 전체 라인: 방향 점 또는 이동하는 점을 함께 표시

모바일에서는 hover가 없으므로 다음 중 하나를 사용한다.

- 첫 번째 탭: 연결된 Relay 쌍 하이라이트
- 두 번째 탭 또는 다른 입력: 실제 선택/연결 진행
- 또는 리소스 선택 상태 동안만 전체 Relay line 표시

## 상태 피드백

### Waiting

기존에는 waiting 상태에서 프로세스 뒤의 검은 배경이 커졌다 작아졌다. 새 방향에서는 프로세스 내부 중앙 검은 점이 커졌다 작아지는 방식으로 바꾼다.

권장 모션:

- 대상: `ProcessPort`
- scale: `1.0 -> 1.18 -> 1.0`
- duration: `0.55s ~ 0.75s`
- easing: 부드러운 sine in/out
- alpha는 크게 흔들지 않고, 크기 변화만으로 상태를 표현한다.

추가 후보:

- 중앙 포트 주변에 아주 얇은 ring pulse를 1개 추가한다.
- 필요 색 트레이의 아직 채워지지 않은 칩이 아주 약하게 breathing한다.
- 연결 가능한 리소스가 있을 때만 해당 색 칩과 리소스 stroke를 살짝 밝힌다.

### 성공 / 연결 완료

- 연결선이 리소스와 프로세스에 닿는 순간 짧은 scale pop을 준다.
- 리소스 내부 슬롯 또는 포트가 해당 색으로 부드럽게 채워진다.
- FEEL을 사용해 성공 사운드, 짧은 카메라 반응, 햅틱을 묶을 수 있다.
- 모바일에서는 시각 효과보다 터치 햅틱과 명확한 상태 변화가 중요하다.

### 실패 / 잘못된 연결

- 연결선 또는 선택된 포트가 짧게 흔들린다.
- 빨간색 전체 flash보다, 해당 입력 지점 주변에서만 짧은 반응을 준다.
- 실패가 잦아도 피곤하지 않게 duration은 짧게 유지한다.

### 선택 / hover

- 선택된 프로세스의 외곽 stroke를 강조한다.
- 선택된 리소스는 shadow를 약간 진하게 하거나 stroke를 밝힌다.
- Relay 대상 리소스는 쌍이 함께 highlight된다.
- 연결 가능한 요구 색 칩과 리소스 색을 동시에 강조한다.

## 구현 메모

- 실제 인게임 구현에서는 프로토타입처럼 한 MonoBehaviour가 전부 생성하지 않는다.
- `ProcessView`, `ResourceView`, `ConnectionView`, `RelayView`, `FeedbackPresenter`, `VisualSettings`로 나눈다.
- Shapes primitive는 하이어라키에서 교체 가능한 단위로 구성한다.
- Relay 끝 장식은 현재 placeholder지만, 나중에 SpriteRenderer 또는 Shapes 기반 전용 아이콘으로 교체할 수 있게 별도 child로 둔다.
- 수치와 색상은 `VisualSettings` ScriptableObject로 빼는 것을 우선 검토한다.
- Domain은 `ResourceType`, `RelayType`, `ConnectionState`, `ProcessState` 같은 상태만 제공하고, 어떤 Shapes를 그릴지는 Presentation이 결정한다.

## 프리팹 하이어라키 기준

실제 인게임 프리팹도 Shapes 프로토타입처럼 의미 단위로 child를 묶는다.

- `Background`: Shadow, Stroke, Fill처럼 노드의 바탕을 이루는 요소
- `Slots`: Capacity 슬롯, Simultaneous 슬롯, ColorSwitch 색 점, 프로세스 필요 색 칩
- `Links`: 일반 연결선, Relay stub, Simultaneous 슬롯과 중앙 허브를 잇는 선
- `Port`: 프로세스/기본 리소스 중앙 포트
- `Icon`: EmptyColor slash, Clock face/hand처럼 규칙 아이콘을 구성하는 요소
- `State`: Clock turn count, Simultaneous activation light처럼 상태값을 직접 보여주는 요소
- `Endpoints`: Relay Link/Transfer 끝 장식 placeholder

이름은 나중에 스프라이트나 별도 View component로 교체하기 쉽도록 기능 기준으로 짓는다. `Shadow`, `Stroke`, `Fill` 같은 렌더링 부품은 루트에 직접 두지 않고 `Background` 아래에 둔다.

## 추후 검토할 비주얼 작업

- EmptyColor의 X placeholder를 빈 물방울/점선 원/후보 슬롯 표현으로 교체한다.
- 연결선이 이어질 때 물방울이 합쳐지는 shader 또는 mesh effect를 테스트한다.
- Relay hover/선택 전체 라인의 fade in/out을 실제 입력과 연결한다.
- Transfer 활성화 시 움직이는 점/물방울을 DOTween 또는 shader로 구현한다.
- Waiting 상태를 `ProcessPort` pulse 중심으로 교체한다.
- 각 리소스 타입 아이콘을 스프라이트로 교체할지, Shapes 절차적 아이콘으로 유지할지 결정한다.
- 밝은 배경/어두운 배경 양쪽에서 리소스 stroke, Relay line, 필요 색 tray의 가독성을 확인한다.

## 검증 정책

문서 수정만으로 Unity 컴파일은 실행하지 않는다. 사용자가 명시적으로 "검증해줘", "컴파일해줘", "Unity에서 확인해줘"라고 요청할 때만 `deadlock-unity-validation-manual` 절차를 사용한다.
