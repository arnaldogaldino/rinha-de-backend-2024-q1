

# cd /mnt/c/_projects/rinha-de-backend-2024-q1/participantes/arnaldogaldino
# docker compose up -d

 wsl -l -v

 wsl --set-version Ubuntu-20.04 2
 wsl --set-default-version 2
 wsl --set-default Ubuntu-20.04

 
// https://github.com/zanfranceschi/rinha-de-backend-2024-q1
// cd /mnt/c/_projects/rinha-de-backend-2024-q1/participantes/arnaldogaldino



# docker tag local-image:tagname new-repo:tagname
# docker push new-repo:tagname

sudo docker system prune -af

docker buildx build --platform linux/amd64 -t arnaldogaldino/rinha-2024q1-crebito:latest -f src/RinhaBackend/Dockerfile .

docker push arnaldogaldino/rinha-2024q1-crebito:latest

docker compose up -d

$env:GATLING_HOME=$env:USERPROFILE\gatling\3.10.3\

	CREATE OR REPLACE FUNCTION gravar_transacao (p_cliente_id INTEGER, p_valor INTEGER, p_tipo CHAR(1), p_descricao VARCHAR(10))
	RETURNS table(limite INTEGER, saldo INTEGER, limite_insuficiente BOOLEAN)
		LANGUAGE plpgsql
	AS $$
	DECLARE v_limite INTEGER := 0;
	DECLARE v_saldo INTEGER := 0;
	DECLARE v_limite_insuficiente BOOLEAN := false;
	BEGIN
		
		SELECT s.valor, c.limite into v_saldo, v_limite
		FROM saldos as s 
		JOIN clientes c on c.id = s.cliente_id
		WHERE s.cliente_id = p_cliente_id;
		
		IF p_tipo = 'd' AND (v_limite*-1) > v_saldo+(p_valor*-1) THEN
			limite_insuficiente := true;
		END IF;

		IF limite_insuficiente IS NOT TRUE THEN		
			INSERT INTO transacoes (cliente_id, valor, tipo, descricao, realizada_em) VALUES (p_cliente_id, p_valor, p_tipo, p_descricao, now());
		END IF;
		
		IF limite_insuficiente IS NOT TRUE and p_tipo = 'c' THEN		
			UPDATE saldos set valor=valor+p_valor where cliente_id=p_cliente_id;
			v_saldo := v_saldo+p_valor;
		END IF;
		
		IF limite_insuficiente IS NOT TRUE and p_tipo = 'd' THEN
			UPDATE saldos set valor=valor+(p_valor*-1) where cliente_id=p_cliente_id;
			v_saldo := v_saldo+(p_valor*-1);
		END IF;
		
		RETURN query SELECT v_limite, v_saldo, v_limite_insuficiente;
	END;
	$$



