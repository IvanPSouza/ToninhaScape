using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class PlayerControl : MonoBehaviourPunCallbacks, IPunObservable
{
    #region Private Fields

    [SerializeField] static GameObject Instance;
    [SerializeField] static GameObject LocalPlayerInstance;
    private Rigidbody2D _rb;
    private TMP_Text _namePlayer;
    [SerializeField] private float _jumpForce = 10f;
    [SerializeField] private float _playerSpeed = 10f;
    private Vector3 networkPosition;
    private string _nickname;
    private bool _isGrounded;

    #endregion

    #region Properties

    public Vector2 Movement { get; set; }
    public float JumpForce => _jumpForce;
    public float PlayerSpeed
    {
        get => _playerSpeed;
        set => _playerSpeed = value;
    }

    #endregion

    #region Vida

    [Range(0f, 100f)]
    [SerializeField] float healthPoints = 100f;
    private SpriteRenderer Color;
    private new Color light;

    #endregion

    private void Awake()
    {
        //if (Instance == null)
        //{
        //    Instance = this.gameObject;
        //}   
    }

    // Start is called before the first frame update
    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _namePlayer = GetComponentInChildren<TMP_Text>();
        Color = GetComponent<SpriteRenderer>();
        light = Color.color;

        if (photonView.IsMine)
        {
            if (LocalPlayerInstance != null) { LocalPlayerInstance = this.gameObject; }
            _nickname = PhotonNetwork.LocalPlayer.NickName;
            _namePlayer.text = _nickname;
        }
        else
        {
            _namePlayer.text = _nickname;
        }
    }

    // Update is called once per frame
    void Update()
    {
        float moveH = Input.GetAxis("Horizontal");
        bool isJumpPressed = Input.GetButtonDown("Jump");
        LifeIndicator();

        // Controle de movimento horizontal (2D)
        Movement = new Vector2(moveH * PlayerSpeed, _rb.velocity.y);

        // Verifica��o do pulo (somente para o jogador local)
        if (isJumpPressed && _isGrounded && photonView.IsMine)
        {
            _rb.velocity = new Vector2(_rb.velocity.x, JumpForce);
        }

        // Para o jogador oponente, desabilitar o pulo (n�o faz nada se n�o for o jogador local)
        if (!photonView.IsMine)
        {
            _rb.velocity = new Vector2(_rb.velocity.x, 0); // Garantir que o jogador remoto n�o pule
        }
    }

    private void FixedUpdate()
    {
        if (photonView.IsMine)
        {
            // Controle do movimento do jogador local
            _rb.velocity = new Vector2(Movement.x, _rb.velocity.y);
        }
        else
        {
            // Para o jogador remoto, apenas suavizar a posi��o, sem influenciar a f�sica (pulo)
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 10);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if player is on the ground when colliding with the ground layer
        if (collision.collider.CompareTag("Ground"))
        {
            _isGrounded = true;
        }
        if (collision.collider.CompareTag("Bullet1"))
        {
            healthPoints -= 10;
            Debug.Log(healthPoints + " Vida total " + _nickname);
            LifeIndicator();
        }
        if (collision.collider.CompareTag("Bullet2"))
        {
            healthPoints -= 25;
            Debug.Log(healthPoints + " Vida total " + _nickname);
            LifeIndicator();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // Player is no longer grounded
        if (collision.collider.CompareTag("Ground"))
        {
            _isGrounded = false;
        }
    }

    private void LifeIndicator()
    {
        Color.color = new Color(light.r, (1f * healthPoints) / 100, (1f * healthPoints) / 100, light.a);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // Enviar posi��o, nickname, grounded, e vida
            stream.SendNext(transform.position);
            stream.SendNext(_nickname);
            stream.SendNext(_isGrounded);
            stream.SendNext(healthPoints); // Sincronizando vida
        }
        else
        {
            // Receber dados
            networkPosition = (Vector3)stream.ReceiveNext();
            _nickname = (string)stream.ReceiveNext();
            _isGrounded = (bool)stream.ReceiveNext();
            healthPoints = (float)stream.ReceiveNext(); // Atualizar a vida

            if (photonView.IsMine)
            {
                _namePlayer.text = PhotonNetwork.LocalPlayer.NickName;
            }
            else
            {
                _namePlayer.text = _nickname;
            }
        }
    }
}
