# Yazılım Geliştirme Laborotuvarı-1 Unity Oyun Projesi "The Last Sheriff".
Proje Özeti: "The Last Sheriff", Unity oyun motoru kullanılarak geliştirilmiş TPS (third person shooter) bir 3D aksiyon-macera oyunudur. Oyuncu, Midtown kasabasının şerifini kontrol ederek kalede esir tutulan prensesi kurtarmaya çalışır. Proje, görev tabanlı ilerleyişi, yapay zeka destekli düşman davranışları ve etkileşimli çevresiyle dinamik bir oyun deneyimi sunar. Proje, hem oyun tasarımı hem de yazılım mimarisi açısından Unity’nin bileşen tabanlı sistemini, modüler kod yapısını ve gerçek zamanlı oyun döngüsünü uygulamalı olarak sergilemeyi amaçlamaktadır.

Projenin Amacı: Bu projenin amacı, Unity üzerinde eksiksiz bir oyun geliştirme sürecini deneyimlemek ve karakter kontrolü, yapay zeka, UI yönetimi, oyun döngüsü ve sahne geçişleri gibi tüm ana bileşenleri içeren bir yapıyı inşa etmektir. Projede; gerçek zamanlı etkileşimli bir 3D ortam, görev tabanlı hikaye akışı, düşman yapay zekası, kazanma ve kaybetme ekranları gibi temel oyun mekaniği bileşenleri uygulanmıştır.

Sistem Mimarisi: 
YazLabProje/Assets/Scripts/Player
-Player.cs: Oyuncunun temel hareket,animasyon ve kontrol mekanizmasını yönetir.
-PlayerDeath.cs:Oyuncunun ölme olayını yönetir ve game over ekranını tetikler.
-WeaponEquip.cs:Oyuncunun silahı donanmasını yönetir.
-WeaponSwitcher.cs:Oyuncunun silahları değiştirmesini yönetir.
-WeaponAttachment.cs:Silahların değişim ve ek parça kontrollerini sağlar.
-DiagnoseEquip.cs:Oyuncunun ekipman etkileşimini yönetir.
-GunShooter.cs:Oyuncunun ateş etme mekaniğini yönetir.
-GunsMenu.cs:Silah seçimini yönetir.
-GunWiev.cs:Silahların görünümünü yönetir.

YazLabProje/Assets/Scripts/Enemy
-EnemyAI.cs:Düşman yapay zekası, takip ve saldırıları yönetir.
-EnemyMelee.cs:Düşmanların yakın dövüşünü yönetir.
-EnemySpawner.cs: Düşmanların sahneye dinamik olarak eklenmesini sağlar.
-EnemyWeaponAttack.cs:Silahlı düşmanların atağını yönetir.
-GuardAimer.cs:Silahlı düşmanların nişan almasını yönetir.
-GuardShooter.cs:Silahlı düşmanların ateş etmesini yönetir.

YazLabProje/Assets/Scripts/Camera
-AimController.cs:Nişan alma ve kamera dönüş kontrolü sağlar.
-TPSCameraSimple.cs:Üçüncü şahıs kamera kontrolü sağlar.

YazLabProje/Assets/Scripts/Healths
-Health.cs:Tüm karakterlerin sağlığını yönetir.
-EnemyHealthBar.cs:Düşmanların sağlık barını yönetir.
-PlayerHealthUI.cs:Oyuncunun sağlık barını yönetir.

YazLabProje/Assets/Scripts/Prisoner
-Prisoner.cs:Kurtarılması gereken Karakterin(NPC) davranışlarını yönetir.

YazLabProje/Assets/Scripts/Panels
-CallGameOverOnDeath.cs Game Over panelini çağırır.
-DieParamToGameOver.cs:Game over panelini çağırır.
-GameOverUI.cs:Game over panelini açar.
-MainMenu.cs:Ana menü panelini oyun başında açar.
-PauseMenu.cs:Oyun duraklatılınca Pause Menü panelini açar.
-Winzone.cs:Oyun kazanılınca Winzone panelini açar.

Oyun Mekanikleri Blok Diyagramı:
┌───────────────────────┐         ┌─────────────────────────────┐          ┌──────────────────────────┐
│     INPUT SYSTEM      │ WASD    │    PLAYER CONTROLLER        │ Animator │  ANIMATION STATE MACHINE │
│ (Klavye + Fare)       │ LShift  │  (Rigidbody tabanlı)        ├─────────►│  Idle/Walk/Run           │
│ - WASD hareket        │ RMB     │  - Yatay hareket (XZ)       │          │  Crouch Idle/Walk        │
│ - LShift koş          │ LMB     │  - Zıplama (Space)          │          │  Pistol Idle/Walk (Aim)  │
│ - C eğil toggle       │ Space   │  - Yer kontrolü (GroundChk) │          │  Jump (inişle çıkış)     │
└──────────┬────────────┘         └──────────────┬──────────────┘          └───────────┬──────────────┘
           │                                      │                                    │
           │ Aim yönü / hareket vektörü           │                                    │
           │                                      │                                    │
           │                                      ▼                                    │
           │                         ┌────────────┴────────────┐                       │
           │                         │   CAMERA FOLLOW / TPS   │                       │
           │                         │  (arkadan takip, offset)│                       │
           │                         └────────────┬────────────┘                       │
           │                                      │                                    │
           │                                      ▼                                    │
           │                         ┌────────────┴────────────┐                       │
           │                         │      WEAPON EQUIP       │                       │
           │                         │  - Grip hizalama        │                       │
           │                         │  - Aim offset           │                       │
           │                         └────────────┬────────────┘                       │
           │                                      │                                    │
           │                                      ▼                                    │
           │                         ┌────────────┴────────────┐   Raycast (hitMask)   │
           └────────────────────────►│       GUN SHOOTER       ├───────────────────────┘
                                     │  - Aim gerekiyorsa ateş │
                                     │  - Muzzle flash / ses   │
                                     └────────────┬────────────┘
                                                  │  Hit
                                                  ▼
                                     ┌────────────┴────────────┐
                                     │  ENEMY HEALTH / RAGDOLL │
                                     │  - Hasar/Ölüm           │
                                     └────────────┬────────────┘
                                                  │ UI güncelle
                                                  ▼
                                     ┌────────────┴────────────┐
                                     │ WORLD-SPACE HEALTH BAR  │
                                     │ (kafanın üstünde)       │
                                     └─────────────────────────┘


            LOS (losMask: Player+Cover+Env)                 Kovalamaca / Ateş
┌───────────────────────────┐     Görüş hattı     ┌───────────────────────────┐
│     ENEMY WEAPON ATTACK   │◄────────────────────│       ENEMY AI (SWAT)     │
│  - DetectRange / FireRate │                     │  - NavMeshAgent Patrol    │
│  - FireOnce (Ray)         │────────────────────►│  - Chase / Stop & Aim     │
│  - Audio / Impact VFX     │   Player’a hasar    │  - Fire (FireRange)       │
└───────────────┬───────────┘                     └───────────────┬───────────┘
                │                                              Death/Disable
                │                                                   │
                ▼                                                   ▼
       ┌────────┴────────┐                                   ┌──────┴─────────┐
       │ PLAYER HEALTH   │ 0 olursa                          │ GAME OVER UI   │
       │ (bar:kafa üstü) ├──────────────────────────────────►│ (Retry / Exit) │
       └────────┬────────┘                                   └────────────────┘
                │ WinZone tetiklenirse
                ▼
       ┌────────┴────────┐
       │   WIN PANEL     │
       │ (Görev yazısı   │
       │  gizlenir)      │
       └─────────────────┘


Ek Bileşenler:
- Physics: Rigidbody hareket, yer kontrolü (ground check), zıplama (Space → rb.AddForce / velocity.y).
- Layers: hitMask (Player/Enemy/Env), losMask (Player+Cover+Env) → Cover hem LOS’u hem mermiyi keser.
- Müzik: MainMenu ve Demo1 sahnesinde sahneye özel AudioSource veya kalıcı MusicManager (cross-fade).


Oyun Sahnesi ve Tasarımı: 1.Main Menu Sahnesi: Oyunun nasıl oynanabileceğinin, oyunun hikayesinin ve tuş kontrollerinin anlatıldığı ana menüdür. Oyuna başlayabilirsiniz veya nasıl oynanacağını öğrenebilirsiniz.
                          2. Demo 1 Sahnesi: Asıl oyun alanı. Çevre, düşmanlar, görev alanları, kazanma ve kaybetme koşulları burada bulunur.
                          
Literatür Taraması ve Karşılaştırma: Proje geliştirilmeden önce benzer temalı oyunlar incelendi. Red Dead Redemption ve Assasins Creed oyunlarının tasarımlarından ilham alındı. Gerçekçi fizik, açık dünya ve NPC AI gibi konularda esinildi. Sonuç olarak, "The Last Sheriff" bu örneklerden ilham almakla birlikte, daha sade, görev odaklı ve eğitim amaçlı bir oyun mimarisi hedeflemiştir.

Kullanılan Teknolojiler ve Teknikler: Oyun motoru: Unity
                                      Kodlama: C#
                                      Kaynak Kontrol: Github
                                      UI: TextMeshPro
                                      AI: NavMesh
                                      Raycast ile hedef tespiti 
                                      NavMesh ile düşman hareketi
                                      LateUpdate() ile kamera kontrolü
                                      Trigger bölgeleri (Winzone ve GameOver gibi)
                                      

Karşılaşılan Zorluklar ve Çözümler: Proje süreci boyunca teknik ve ekip içi anlamda birçok zorlukla karşılaştık. Her bir sorun, oyun geliştirmenin gerçek dünyadaki karmaşıklığını anlamamıza yardımcı oldu ve bizi daha bilinçli geliştiriciler haline getirdi. Yaşadığımız bazı zorluklar:

Silahın karakterin elinde doğru görünmemesi / poz hataları.
Çözüm: Kullanılan silahın prefabına grip diye bir empty oluşturuldu. Düzenlenen prefab karakterin sağ elinin içinde oluşturulan RightHand_Mount içine atılarak silah karakterin eline orantılı yerleştirildi. Sonrasında play ekranında rotation ve position ayarları uygun konuma getirilene kadar düzenlendi. Uygun konuma gelince değerler kaydedilip edit ekranında kaydedildi.

Collider ve fizik etkileşim hataları.
Çözüm:Zeminlere Mesh Collider, oyuncuya Capsule Collider eklendi; isTrigger ayarları kontrol edildi. Düşman mermilerinde Hit Mask ve Cover Layer optimizasyonu yapıldı.

WinZone Panel Sorunu.
Çözüm: WinZone objesine Box Collider eklendi, Is Trigger aktif hale getirildi. Player objesinin "tag"i Player olarak düzeltildi.

Düşman AI Devriye/Takip Kopmaları.
Çözüm: Navigation penceresinden sahneyi yeniden Bake edildi. NavMeshAgent için hız, acceleration, angular speed dengesi ayarlandı. 

Gelişimler ve Kazanımlar: Unity’de tam fonksiyonel bir oyun döngüsü oluşturma becerisi.
                          Git üzerinden ekipli çalışma pratiği.
                          Kod modülerleştirme ve hata ayıklama disiplini.
                          Yapay zeka davranışlarının temellerini öğrenme.
                          

Ekip Üyeleri: Demir Demirkan
              Efe Yılmaz. 
              Yusuf Çelebi
                                    
