using UnityEngine;

/// <summary>
/// Arraste este asset para Resources/ e atribua as texturas de transição no inspector.
/// As texturas podem ficar em qualquer pasta do projeto (ex: Assets/RPG/Battle Transitions/).
///
/// Criar: clique com o botão direito no Project → Create → Battle → Transition Config
/// Salvar em: Assets/Resources/BattleTransitionConfig.asset
/// </summary>
[CreateAssetMenu(fileName = "BattleTransitionConfig", menuName = "Battle/Transition Config")]
public class BattleTransitionConfig : ScriptableObject
{
    [Header("Texturas de Gradiente (preto = transiciona primeiro, branco = por último)")]
    public Texture2D verticalReflectedWipe;
    public Texture2D chessThenCircles;
    public Texture2D circlesChessMoreCircles;
    public Texture2D enclosingTriangles;
    public Texture2D spinningSpiral;
    public Texture2D gooey;
    public Texture2D trapped;
    public Texture2D crashingWaves;

    public Texture2D GetTexture(BattleTransitionType type)
    {
        switch (type)
        {
            case BattleTransitionType.VerticalReflectedWipe:   return verticalReflectedWipe;
            case BattleTransitionType.ChessThenCircles:        return chessThenCircles;
            case BattleTransitionType.CirclesChessMoreCircles: return circlesChessMoreCircles;
            case BattleTransitionType.EnclosingTriangles:      return enclosingTriangles;
            case BattleTransitionType.SpinningSpiral:          return spinningSpiral;
            case BattleTransitionType.Gooey:                   return gooey;
            case BattleTransitionType.Trapped:                 return trapped;
            case BattleTransitionType.CrashingWaves:           return crashingWaves;
            default:                                           return null;
        }
    }
}
